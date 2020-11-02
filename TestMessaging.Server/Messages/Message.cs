using TestMessaging.DAL.Entities;
using TestMessaging.Server.Messages.Enums;

namespace TestMessaging.Server.Messages
{
    public class Message
    {
        public string Token { get; set; }
        public MessageType MessageType { get; set; } 
        public string UserName { get; set; }
        public MessageEntity[] Payload { get; set; }
    }
}