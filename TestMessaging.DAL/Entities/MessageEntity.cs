using System;

namespace TestMessaging.DAL.Entities
{
    public class MessageEntity
    {
        public DateTime TimeStamp { get; set; }
        public string UserName { get; set; }
        public string Text { get; set; }
    }
}