using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace TestMessaging.Server.Processors
{
    public interface IUserSocketProcessor : IDisposable
    {
        Task StartMessageProcessing(WebSocket socket, CancellationToken token);
    }
}