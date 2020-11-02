using System;
using System.Text;
using Newtonsoft.Json;

namespace TestMessaging.Common.Extensions
{
    public static class MessageExtensions
    {
        private static readonly JsonSerializerSettings _defaultSerializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };

        public static T To<T>(this ReadOnlyMemory<byte> body)
        {
            var messageJson = Encoding.UTF8.GetString(body.ToArray());
            var message = JsonConvert.DeserializeObject<T>(messageJson, _defaultSerializerSettings);
            return message;
        }

        public static ReadOnlyMemory<byte> GetBytes<T>(this T message)
        {
            var messageJson = JsonConvert.SerializeObject(message, _defaultSerializerSettings);
            var body = Encoding.UTF8.GetBytes(messageJson);
            return body;
        }
    }
}