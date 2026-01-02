using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Server.Network
{
    /// <summary>
    /// 纯网络会话管理：创建/索引/回收会话；不触达业务与Actor。
    /// 注意：不再内部绑定 MessageDispatcher（由上层订阅 ISession.OnPacketReceived）。
    /// </summary>
    public class SessionManager : IDisposable
    {
        private readonly ConcurrentDictionary<EndPoint, ISession> sessions = new();
        private readonly ConcurrentDictionary<Guid, EndPoint> sessionIdToEndPoint = new();

        public int ConnectionCount => sessions.Count;

        /// <summary>
        /// 创建或返回已存在的会话（按 EndPoint 去重）
        /// </summary>
        public ISession CreateSession(ITransport transport, EndPoint endpoint)
        {
            if (sessions.TryGetValue(endpoint, out var existing))
                return existing;

            var ses = new Session(transport, endpoint);
            sessions[endpoint] = ses;
            sessionIdToEndPoint[ses.Id] = endpoint;
            return ses;
        }


        /// <summary>
        /// 根据会话ID移除会话；返回被移除的会话以便上层触发 OnSessionClosed → 通知 SessionActor
        /// </summary>
        public bool RemoveSession(Guid sessionId, out ISession removed)
        {
            removed = null;
            if (!sessionIdToEndPoint.TryGetValue(sessionId, out var ep)) return false;

            if (sessions.TryRemove(ep, out var ses))
            {
                removed = ses;
                ses.Close();

                sessionIdToEndPoint.TryRemove(sessionId, out _);
                return true;
            }

            sessionIdToEndPoint.TryRemove(sessionId, out _);
            return false;
        }

        public bool TryGetSession(Guid sessionId, out ISession session)
        {
            session = null;
            return sessionIdToEndPoint.TryGetValue(sessionId, out var ep)
                   && sessions.TryGetValue(ep, out session);
        }


        public IEnumerable<ISession> GetAllSessions() => sessions.Values;
        public IEnumerable<Guid> GetAllSessionIds() => sessionIdToEndPoint.Keys;

        public void Dispose()
        {
            foreach (var kv in sessions.ToArray())
            {
                kv.Value.Close();
                sessions.TryRemove(kv.Key, out _);
            }
            sessionIdToEndPoint.Clear();
        }

        #region Inner Session

        private sealed class Session : ISession
        {
            private readonly ITransport _transport;
            private readonly ProtocolParser _parser = new();

            public Guid Id { get; } = Guid.NewGuid();
            public EndPoint RemoteEndPoint { get; }
            public SessionState State { get; set; } = SessionState.Connected;
            public string PlayerId { get; set; } = string.Empty;
            public DateTime LastHeartbeat { get; private set; } = DateTime.UtcNow;

            public event Func<GamePacket, Task> OnPacketReceived;

            public Session(ITransport transport, EndPoint remoteEndPoint)
            {
                _transport = transport ?? throw new ArgumentNullException(nameof(transport));
                RemoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
            }

            public async Task SendAsync(ReadOnlyMemory<byte> data)
            {
                await _transport.SendAsync(RemoteEndPoint, data);
            }

            /// <summary>
            /// 由上层（GameServer 的 OnDataReceived）调用：切包→抛事件
            /// </summary>
            public async Task HandleReceivedData(ReadOnlyMemory<byte> data)
            {
                foreach (var pkt in _parser.ParseData(data))
                {
                    UpdateHeartbeat();
                    if (OnPacketReceived != null)
                    {
                        await OnPacketReceived.Invoke(pkt);
                    }
                }
            }

            public void UpdateHeartbeat() => LastHeartbeat = DateTime.UtcNow;

            public void Close()
            {
                if (State == SessionState.Disconnected) return;
                State = SessionState.Disconnected;
                _transport.NotifyConnectionClosed(RemoteEndPoint);
            }
        }

        #endregion
    }
}