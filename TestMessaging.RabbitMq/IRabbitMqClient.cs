using System;
using TestMessaging.Common;

namespace TestMessaging.RabbitMq
{
    public interface IRabbitMqClient<T> : IMessageConsumer<T>, IMessagePublisher<T>, IDisposable where T : class, new()
    {
        
    }
}