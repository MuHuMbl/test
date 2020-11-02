using System.Threading.Tasks;
using TestMessaging.Server.Messages;
using TestMessaging.Server.Messages.Enums;

namespace TestMessaging.Server.Processors
{
    public interface IMessageProcessor
    {
        MessageType MessageType { get; }

        Task<Message> GetResponseAsync(Message message);
    }
}