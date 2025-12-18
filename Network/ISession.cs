using System;
using System.Net;
using System.Threading.Tasks;

namespace Server.Network
{
    public enum SessionState
    {
        Disconnected,
        Connected,
        Authenticated,
        InGame
    }

    public interface ISession
    {
        Guid Id { get; }
        EndPoint RemoteEndPoint { get; }
        SessionState State { get; set; }
        string PlayerId { get; set; }
        DateTime LastHeartbeat { get; }

        Task SendAsync(ReadOnlyMemory<byte> data);

        void Close();

        void UpdateHeartbeat();

        /// <summary>GameServer 将原始数据交给 Session 切包，并回调 OnPacketReceived</summary>
        Task HandleReceivedData(ReadOnlyMemory<byte> data);

        /// <summary>完成拆包后的回调，GameServer 订阅它并统一转发给 SessionActor</summary>
        event Func<GamePacket, Task> OnPacketReceived;
    }
}