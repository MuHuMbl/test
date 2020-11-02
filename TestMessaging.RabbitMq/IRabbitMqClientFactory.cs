namespace TestMessaging.RabbitMq
{
    public interface IRabbitMqClientFactory
    {
        IRabbitMqClient<T> CreateClient<T>() where T : class, new();
    }
}