using System;

namespace TestMessaging.Common
{
    public class MessageReceivedEventArgs
    {
        public DateTime TimeStamp { get; set; }
        public string UserName { get; set; }
        public string Text { get; set; }
    }
}