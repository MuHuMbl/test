using System;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace TestMessaging.RabbitMq
{
    public class RabbitMqClientFactory : IRabbitMqClientFactory
    {
        private readonly RabbitMqConfiguration _configuration;

        public RabbitMqClientFactory(IOptions<RabbitMqConfiguration> configuration)
        {
            _configuration = configuration.Value;
        }

        public IRabbitMqClient<T> CreateClient<T>() where T : class, new()
        {
            var factory = new ConnectionFactory() {Uri = new Uri(_configuration.ConnectionString)};
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            //Поскольку никакого роутинга нам сейчас не нужно, можно просто использовать fanout
            channel.ExchangeDeclare(_configuration.ExchangeName, durable: true, type: "fanout", autoDelete: false, arguments: null);
            var queueDeclareResult = channel.QueueDeclare();
            channel.QueueBind(queueDeclareResult.QueueName, _configuration.ExchangeName, "");

            return new RabbitMqClient<T>(connection, channel, _configuration.ExchangeName, queueDeclareResult.QueueName);
        }
    }
}