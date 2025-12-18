// Server/Network/ITransport.cs
using System;
using System.Net;
using System.Threading.Tasks;

namespace Server.Network
{
    /// <summary>
    /// 纯网络传输接口。只做 I/O 与连接生命周期事件，不关心会话/业务。
    /// </summary>
    public interface ITransport : IDisposable
    {
        Task StartAsync();
        void Stop();

        Task SendAsync(EndPoint endpoint, ReadOnlyMemory<byte> data);

        /// <summary>新连接被接受（若底层无法探知，可不触发；则由首包到达时上层创建会话）</summary>
        event Action<EndPoint> OnConnectionAccepted;

        /// <summary>收到数据（上抛到 GameServer → Session.HandleReceivedData）</summary>
        event Func<EndPoint, ReadOnlyMemory<byte>, Task> OnDataReceived;

        /// <summary>连接关闭（底层异常、主动断开等）</summary>
        event Func<EndPoint, Task> OnConnectionClosed;

        /// <summary>通知底层关闭指定连接（由 ISession.Close 调用）</summary>
        void NotifyConnectionClosed(EndPoint endpoint);
    }
}
