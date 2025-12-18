using System;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Server.Game.Actor.Core
{
    public class ActorEventBus
    {
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, Subscriber>> subs = new();
        private readonly IActorSystem actorSystem;

        public ActorEventBus(IActorSystem actorSystem)
        {
            this.actorSystem = actorSystem;
        }

        private class Subscriber
        {
            public string ActorId { get; init; }
            public Channel<IActorMessage> Channel { get; init; }
            public CancellationTokenSource Cts { get; init; } = new();
            public Task PumpTask { get; set; }
            public DateTime LastActiveTime { get; set; } = DateTime.UtcNow;
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe<T>(string subscriberActorId, int capacity = 32, bool latestOnly = false) where T : IActorMessage
        {
            // Console.WriteLine($"[EventBus] 订阅请求: Actor={subscriberActorId}, Event={typeof(T).Name}");
            var eventType = typeof(T);
            var map = subs.GetOrAdd(eventType, _ => new ConcurrentDictionary<string, Subscriber>());
            if (map.ContainsKey(subscriberActorId)) return;

            var channelOptions = new BoundedChannelOptions(Math.Max(1, capacity))
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = latestOnly ? BoundedChannelFullMode.DropOldest : BoundedChannelFullMode.Wait
            };



            var ch = Channel.CreateBounded<IActorMessage>(channelOptions);
            var cts = new CancellationTokenSource();

            var subscriber = new Subscriber
            {
                ActorId = subscriberActorId,
                Channel = ch,
                Cts = cts,
                LastActiveTime = DateTime.UtcNow
            };
            
            subscriber.PumpTask = StartMessagePump(subscriber);

            if (!map.TryAdd(subscriberActorId, subscriber))
            {
                cts.Cancel();
                ch.Writer.TryComplete();
                Console.WriteLine($"订阅者 {subscriberActorId} 添加失败，可能已存在");
            }
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public void Unsubscribe<T>(string actorId) where T : IActorMessage
        {
            var eventType = typeof(T);
            if (!subs.TryGetValue(eventType, out var map)) return;
            if (!map.TryRemove(actorId, out var sub)) return;
            try
            {
                sub.Cts.Cancel();
                sub.Channel.Writer.TryComplete();

                if (sub.PumpTask != null && !sub.PumpTask.IsCompleted)
                {
                    sub.PumpTask.Wait(TimeSpan.FromSeconds(5));
                }
            }
            catch (OperationCanceledException) { }
        }


        public async ValueTask PublishAsync(IActorMessage eventMessage)
        {

            var eventType = eventMessage.GetType();

            if (!subs.TryGetValue(eventType, out var map) || map.IsEmpty)
                return;
            // Console.WriteLine($"[EventBus] 发布事件: {eventMessage.GetType().Name}, 订阅者数量: {map.Count}");
            if (map.Count == 1)
            {
                await SendToSingleSubscriber(map.Values.First(), eventMessage);
                return;
            }

            var tasks = new List<ValueTask>();
            foreach (var subscriber in map.Values)
            {
                if (subscriber.Cts.IsCancellationRequested)
                {
                    // 清理无效订阅者
                    map.TryRemove(subscriber.ActorId, out _);
                    continue;
                }

             
                tasks.Add(SendToSubscriber(subscriber, eventMessage));
            }

            // 并行发送，但不等待所有完成（fire-and-forget模式）
            _ = Task.WhenAll(tasks.Select(t => t.AsTask()));
        }

        private async ValueTask SendToSubscriber(Subscriber subscriber, IActorMessage message)
        {
            try
            {
                if (await subscriber.Channel.Writer.WaitToWriteAsync(subscriber.Cts.Token))
                {
                    subscriber.Channel.Writer.TryWrite(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"向订阅者 {subscriber.ActorId} 发送消息失败: {ex}");
            }
        }

        private async ValueTask SendToSingleSubscriber(Subscriber subscriber, IActorMessage message)
        {
            try
            {
                await subscriber.Channel.Writer.WriteAsync(message, subscriber.Cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"向单个订阅者 {subscriber.ActorId} 发送消息失败: {ex}");
            }
        }

        public void CleanupInactiveSubscribers(TimeSpan inactivityThreshold)
        {
            var cutoffTime = DateTime.UtcNow - inactivityThreshold;

            foreach (var eventMap in subs.Values)
            {
                var inactiveSubscribers = eventMap.Where(kv =>
                    kv.Value.LastActiveTime < cutoffTime ||
                    !actorSystem.IsActorAlive(kv.Key)).ToList();

                foreach (var (actorId, subscriber) in inactiveSubscribers)
                {
                    eventMap.TryRemove(actorId, out _);
                    subscriber.Cts.Cancel();
                    Console.WriteLine($"清理无效订阅者: {actorId}");
                }
            }
        }

        private void UnsubscribeByType(string actorId, Type eventType)
        {
            if (subs.TryGetValue(eventType, out var map))
            {
                map.TryRemove(actorId, out var subscriber);
                subscriber?.Cts.Cancel();
            }
        }

        private async Task StartMessagePump(Subscriber subscriber)
        {
            var reader = subscriber.Channel.Reader;
            var token = subscriber.Cts.Token;

            try
            {
                await foreach (var message in reader.ReadAllAsync(token))
                {
                    subscriber.LastActiveTime = DateTime.UtcNow;

                    try
                    {
                        var actor = actorSystem.GetActor(subscriber.ActorId);
                        if (actor != null)
                        {
                            await actor.TellAsync(subscriber.ActorId, message);
                        }
                        else
                        {

                            UnsubscribeByType(subscriber.ActorId, message.GetType());
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"处理事件消息时发生错误 ActorId: {subscriber.ActorId}");
                    }
                }
            }catch(Exception ex)
            {
                Console.WriteLine($"[Pump] {subscriber.ActorId} 消息泵异常: {ex}");
            }

        }
    }
}