using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Server.Network
{
    using MessagePack;
    using Server.Game.Contracts.Network;
    using Server.Utility;
    using System.Text;

    /// <summary>
    /// 游戏网络服务器：底层传输、会话管理与协议分发注册。
    /// 在 Actor 化架构下，不再直连业务，只负责把包交给 SessionActor。
    /// </summary>
    public class GameServer : IDisposable
    {
        private readonly ITransport transport;
        private readonly SessionManager sessionManager;

        // 协议号 → handler
        private readonly Dictionary<ushort, Func<GamePacket, ISession, Task>> handlers =
            new Dictionary<ushort, Func<GamePacket, ISession, Task>>();

        // sessionId → session 层 packet 回调（用于正确取消订阅）
        private readonly Dictionary<Guid, Func<GamePacket, Task>> packetHandlersBySession = new();

        // 避免重复触发 OnSessionOpened（并发安全）
        private readonly ConcurrentDictionary<Guid, byte> openedSessions = new();

        public bool IsRunning { get; private set; }

        public event Func<ISession, Task> OnSessionOpened;
        public event Func<Guid, string, Task> OnSessionClosed;

        public GameServer(ITransport transport, SessionManager sessionManager)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));

            transport.OnConnectionAccepted += HandleConnectionAccepted;
            transport.OnDataReceived += HandleDataReceived;
            transport.OnConnectionClosed += HandleConnectionClosed;
        }

        #region 启停

        public async Task StartAsync()
        {
            if (IsRunning) return;
            IsRunning = true;
            await transport.StartAsync();
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            transport.Stop();

            foreach (var sid in sessionManager.GetAllSessionIds())
            {
                if (sessionManager.RemoveSession(sid, out var removed))
                {
                    openedSessions.TryRemove(sid, out _);
                    _ = SafeInvokeSessionClosed(sid, "server stop");
                }
            }
        }

        private async Task SafeInvokeSessionClosed(Guid sessionId, string reason)
        {
            try
            {
                if (OnSessionClosed != null)
                {
                    await OnSessionClosed.Invoke(sessionId, reason);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnSessionClosed handler: {ex}");
            }
        }

        private async Task SafeInvokeSessionOpened(ISession ses)
        {
            try
            {
                if (OnSessionOpened != null)
                {
                    await OnSessionOpened.Invoke(ses);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnSessionOpened handler: {ex}");
            }
        }

        #endregion

        #region Handler 注册

        public void RegisterHandler(ushort protocolId, Func<GamePacket, ISession, Task> handler)
        {
            handlers[protocolId] = handler;
        }

        public void RegisterHandler(Protocol protocol, Func<GamePacket, ISession, Task> handler)
        {
            handlers[(ushort)protocol] = handler;
        }


        #endregion

        #region 发送 API（给 NetworkGatewayActor 调用）

        public async Task SendTo(Guid sessionId, Protocol protocolId, byte[] bytes)
        {
            if (sessionManager.TryGetSession(sessionId, out var ses))
            {
                var packet = CreateGamePacket(protocolId, bytes);
                await ses.SendAsync(packet.SerializePacket());
            }
        }


        public string ToHex(ReadOnlyMemory<byte> mem)
        {
            var span = mem.Span;
            StringBuilder sb = new StringBuilder(span.Length * 3);
            for (int i = 0; i < span.Length; i++)
            {
                sb.AppendFormat("{0:X2} ", span[i]);
            }
            return sb.ToString();
        }



        private GamePacket CreateGamePacket(Protocol protocol, byte[] bytes)
        {
            return new GamePacket((ushort)protocol, bytes);
        }

        #endregion

        #region 底层事件

        private void HandleConnectionAccepted(EndPoint ep)
        {
            var ses = sessionManager.CreateSession(transport, ep);

            if (openedSessions.TryAdd(ses.Id, 0))
                _ = SafeInvokeSessionOpened(ses);

            if (!packetHandlersBySession.ContainsKey(ses.Id))
            {
                // 会话专属分发器（并保存引用，便于取消订阅）
                Func<GamePacket, Task> cb = async pkt =>
                {
                    if (handlers.TryGetValue(pkt.ProtocolId, out var handler))
                        await handler(pkt, ses);
                    else
                        Console.WriteLine($"未注册的协议 {pkt.ProtocolId}");
                };

                packetHandlersBySession[ses.Id] = cb;
                ses.OnPacketReceived += cb;
            }
        }

        private async Task HandleDataReceived(EndPoint ep, ReadOnlyMemory<byte> buffer)
        {
            try
            {
                var ses = sessionManager.CreateSession(transport, ep);
                if (openedSessions.TryAdd(ses.Id, 0))
                {
                    await SafeInvokeSessionOpened(ses);
                }

                await ses.HandleReceivedData(buffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleDataReceived: {ex}");
            }
        }

        private async Task HandleConnectionClosed(EndPoint ep)
        {
            try
            {
                foreach (var sid in sessionManager.GetAllSessionIds())
                {
                    if (sessionManager.RemoveSession(sid, out var removed) &&
                        removed.RemoteEndPoint.Equals(ep))
                    {
                        if (packetHandlersBySession.Remove(sid, out var cb))
                        {
                            removed.OnPacketReceived -= cb;
                        }

                        openedSessions.TryRemove(sid, out _);
                        await SafeInvokeSessionClosed(sid, "connection closed");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleConnectionClosed: {ex}");
            }
        }

        #endregion

        public void Dispose()
        {
            Stop();
            transport.Dispose();
            sessionManager.Dispose();
        }
    }
}