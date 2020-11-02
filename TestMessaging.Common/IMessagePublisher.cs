namespace TestMessaging.Common
{
    public interface IMessagePublisher<T> where T : class, new()
    {
        void Publish(T message);
    }
}