using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Network
{
    public sealed class TcpTransport : ITransport
    {
        private readonly IPEndPoint listenEndPoint;
        private Socket listener;
        private readonly ConcurrentDictionary<EndPoint, Socket> clients = new();
        private CancellationTokenSource cts;

        public event Action<EndPoint> OnConnectionAccepted;
        public event Func<EndPoint, ReadOnlyMemory<byte>, Task> OnDataReceived;
        public event Func<EndPoint, Task> OnConnectionClosed;

        public TcpTransport(IPEndPoint listenEndPoint)
        {
            this.listenEndPoint = listenEndPoint ?? throw new ArgumentNullException(nameof(listenEndPoint));
        }

        public async Task StartAsync()
        {
            cts = new CancellationTokenSource();

            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };
            listener.Bind(listenEndPoint);
            listener.Listen(512);

            _ = Task.Run(AcceptLoop, cts.Token);
            await Task.CompletedTask;
        }

        public void Stop()
        {
            try { cts?.Cancel(); } catch { }

            foreach (var kv in clients)
            {
                try { kv.Value.Shutdown(SocketShutdown.Both); } catch { }
                try { kv.Value.Close(); } catch { }
            }
            clients.Clear();

            try { listener?.Close(); } catch { }
            listener = null;
        }

        public async Task SendAsync(EndPoint endpoint, ReadOnlyMemory<byte> data)
        {
            if (!clients.TryGetValue(endpoint, out var socket)) return;
            try
            {
                await socket.SendAsync(data, SocketFlags.None);
            }
            catch
            {
                // 发送异常按断开处理
                await CloseClient(endpoint, socket, raiseEvent: true);
            }
        }

        public void NotifyConnectionClosed(EndPoint endpoint)
        {
            if (clients.TryRemove(endpoint, out var socket))
            {
                _ = CloseClient(endpoint, socket, raiseEvent: true);
            }
        }

        private async Task AcceptLoop()
        {
            while (!cts.IsCancellationRequested)
            {
                Socket client = null;
                try
                {
                    client = await listener.AcceptAsync(cts.Token);
                    client.NoDelay = true;

                    var ep = client.RemoteEndPoint;
                    if (ep == null)
                    {
                        try { client.Close(); } catch { }
                        continue;
                    }

                    clients[ep] = client;
                    try { OnConnectionAccepted?.Invoke(ep); } catch { }

                    _ = Task.Run(() => ReceiveLoop(ep, client), cts.Token);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception)
                {
                    try { client?.Close(); } catch { }
                }
            }
        }

        private async Task ReceiveLoop(EndPoint ep, Socket socket)
        {
            var buf = new byte[64 * 1024];
            while (!cts.IsCancellationRequested)
            {
                int read = 0;
                try
                {
                    read = await socket.ReceiveAsync(buf, SocketFlags.None, cts.Token);
                    if (read <= 0) throw new SocketException((int)SocketError.ConnectionReset);

                    if (OnDataReceived != null)
                    {
                        await OnDataReceived.Invoke(ep, new ReadOnlyMemory<byte>(buf, 0, read));
                    }
                }
                catch (OperationCanceledException) { break; }
                catch
                {
                    // 断开
                    await CloseClient(ep, socket, raiseEvent: true);
                    break;
                }
            }
        }

        private async Task CloseClient(EndPoint ep, Socket socket, bool raiseEvent = false)
        {
            try { socket.Shutdown(SocketShutdown.Both); } catch { }
            try { socket.Close(); } catch { }

            clients.TryRemove(ep, out _);

            if (raiseEvent && OnConnectionClosed != null)
            {
                try { await OnConnectionClosed.Invoke(ep); } catch { }
            }
        }

        public void Dispose() => Stop();
    }
}