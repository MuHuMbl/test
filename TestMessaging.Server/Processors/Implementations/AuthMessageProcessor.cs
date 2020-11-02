using System;
using System.Threading.Tasks;
using TestMessaging.Common;
using TestMessaging.Common.Extensions;
using TestMessaging.DAL.Entities;
using TestMessaging.DAL.Repositories;
using TestMessaging.Server.Messages;
using TestMessaging.Server.Messages.Enums;

namespace TestMessaging.Server.Processors.Implementations
{
    public class AuthMessageProcessor : IMessageProcessor
    {
        private readonly IUserRepository _userRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly TokenGenerator _tokenGenerator;
        private readonly IMessagePublisher<MessageReceivedEventArgs> _messagePublisher;


        public AuthMessageProcessor(IUserRepository userRepository, IMessageRepository messageRepository, IMessagePublisher<MessageReceivedEventArgs> messagePublisher, TokenGenerator tokenGenerator)
        {
            _userRepository = userRepository;
            _tokenGenerator = tokenGenerator;
            _messageRepository = messageRepository;
            _messagePublisher = messagePublisher;
        }

        public MessageType MessageType => MessageType.Auth;
        public async Task<Message> GetResponseAsync(Message message)
        {
            var user = await _userRepository.GetUserByNameAsync(message.UserName).ConfigureAwait(false);

            if (user == null)
            {
                var token = _tokenGenerator.GenerateToken(message.UserName);

                user = new UserEntity
                {
                    Token = token,
                    UserName = message.UserName
                };

                await _userRepository.CreateUser(user);
            }

            var messageHistory = await _messageRepository.GetHistoryAsync().ConfigureAwait(false);

            _messagePublisher.Publish(new MessageReceivedEventArgs
            {
                Text = "Entered chat",
                TimeStamp = DateTime.UtcNow,
                UserName = user.UserName
            });

            return new Message
            {
                Token = user.Token,
                MessageType = MessageType.History,
                UserName = user.UserName,
                Payload = messageHistory
            };


        }
    }
}