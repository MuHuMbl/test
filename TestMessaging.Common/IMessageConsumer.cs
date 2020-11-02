using System;

namespace TestMessaging.Common
{
    public interface IMessageConsumer<T> where T : class, new()
    {
        event EventHandler<T> NewMessageReceived;
    }
}