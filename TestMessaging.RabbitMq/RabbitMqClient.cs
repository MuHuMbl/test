using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TestMessaging.Common.Extensions;

namespace TestMessaging.RabbitMq
{
    //Не идеальное исполнение
    public class RabbitMqClient<T> : IRabbitMqClient<T> where T : class, new()
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly EventingBasicConsumer _consumer;
        private readonly string _exchangeName;

        public RabbitMqClient(IConnection connection, IModel channel, string exchangeName, string queueName)
        {
            _exchangeName = exchangeName;
            _connection = connection;
            _channel = channel;

            _consumer = new EventingBasicConsumer(channel);
            _consumer.Received += Consumer_Received;
            channel.BasicConsume(queue: queueName, autoAck: false, consumer: _consumer);
        }

        private void Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            var message = e.Body.To<T>();
            var handler = NewMessageReceived;
            handler?.Invoke(this, message);
            _channel.BasicAck(deliveryTag: e.DeliveryTag, multiple: false);
        }

        public event EventHandler<T> NewMessageReceived;

        public void Dispose()
        {
            _consumer.Received -= Consumer_Received;
            _connection?.Dispose();
            _channel?.Dispose();
        }

        public void Publish(T message)
        {
            var body = message.GetBytes();
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(_exchangeName, "", basicProperties: properties, body: body);
        }
    }
}