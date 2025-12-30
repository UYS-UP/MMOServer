using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.Ocsp;
using Server.Game.Actor.Domain.Gateway;
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

        public virtual async Task Initialize(IActorSystem system)
        {
            System = system;
      
            try
            {
                await OnStart();
                IsRunning = true;
                _ = Task.Run(async () => { 
                    try {
                        await MessageProcessingLoop();
                    }
                    catch { throw; }
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ActorBase] 初始化失败 ActorId={ActorId}: {ex}");
                throw;
            }
        }

        // 启动时的初始化
        protected virtual Task OnStart()
        {
            Console.WriteLine($"Start: {ActorId}");
            return Task.CompletedTask;
        }

        // 停止时的清理
        protected virtual Task OnStop()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnError(Exception ex, object message)
        {
            Console.WriteLine($"[Actor:{ActorId}] OnError: {ex}");
            return Task.CompletedTask;
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
            await TellAsync(nameof(GatewayActor), message);
        }

        // 子类实现消息处理逻辑
        protected abstract Task OnReceive(IActorMessage message);

        public async Task Stop()
        {
            cts.Cancel();
            await OnStop();
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

            await OnStop();
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
                        await OnReceive(message).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await OnError(ex, message);
                    }
                }
            }
            catch (OperationCanceledException) { /* normal shutdown */ }
        }
    }
}