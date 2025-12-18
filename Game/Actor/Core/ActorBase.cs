using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.Ocsp;
using Server.Game.Actor.Domain.Time;
using Server.Game.Actor.Network;
using Server.Game.Contracts.Actor;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Server.Game.Actor.Core
{
    public abstract class ActorBase
    {
        public string ActorId { get; private set; }
        public string Path { get; private set; }

        // 消息队列
        private readonly Channel<IActorMessage> messageChannel;
        private readonly CancellationTokenSource cts = new();
        // 管理 ActorSystem
        public IActorSystem System { get; private set; }
        public bool IsRunning { get; private set; }

        public ActorBase(string actorId, int messageCapacity = 2000)
        {
            ActorId = actorId;
            Path = $"/{actorId}";
            var channelOptions = new BoundedChannelOptions(messageCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            };
            messageChannel = Channel.CreateBounded<IActorMessage>(channelOptions);
        }

        public virtual void Initialize(IActorSystem system)
        {
            System = system;
            IsRunning = true;
            try
            {
                Task.Run(MessageProcessingLoop, cts.Token);
                OnStart();  // 关键！
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ActorBase] 初始化失败 ActorId={ActorId}: {ex}");
                throw;  // 重新抛出，让 CreateActor 感知
            }
        }

        // 启动时的初始化
        protected virtual void OnStart()
        {

        }

        // 停止时的清理
        protected virtual void OnStop()
        {

        }

        protected virtual void OnError(Exception ex, object message)
        {
            Console.WriteLine($"[Actor:{ActorId}] OnError: {ex}");
        }


        // 发送消息（指定接收者）
        public async ValueTask TellAsync(string receiver, IActorMessage message)
        {
            var receiverActor = System.GetActor(receiver);
            if (receiverActor == null)
            {
                Console.WriteLine($"未找到 Actor {receiver}");
                return;
            }
            try
            {
                await receiverActor.messageChannel.Writer.WriteAsync(message, cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"向 Actor {receiver} 发送消息失败: {ex}");
            }
        }

        public async ValueTask TellGateway(IActorMessage message)
        {
            await TellAsync(nameof(NetworkGatewayActor), message);
        }

        // 子类实现消息处理逻辑
        protected abstract Task OnReceive(IActorMessage message);

        public void Stop()
        {
            cts.Cancel();
            OnStop();
        }

        public async Task StopAsync()
        {
            if (!IsRunning) return;

            IsRunning = false;
            cts.Cancel();

            messageChannel.Writer.Complete();

            try
            {
                await Task.Delay(100); // 给处理循环一点时间退出
            }
            catch (OperationCanceledException) { }

            OnStop();
        }

        // 消息处理循环
        private async Task MessageProcessingLoop()
        {
            var reader = messageChannel.Reader;
            try
            {
                await foreach (var message in reader.ReadAllAsync(cts.Token))
                {
                    if (!IsRunning) break;
                    try
                    {
                        await OnReceive(message);
                    }
                    catch (Exception ex)
                    {
                        OnError(ex, message);
                    }
                }
            }
            catch (OperationCanceledException) { /* normal shutdown */ }
        }
    }
}