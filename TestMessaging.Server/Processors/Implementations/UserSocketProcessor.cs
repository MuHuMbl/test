using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TestMessaging.Common;
using TestMessaging.Common.Extensions;
using TestMessaging.Server.Messages;
using TestMessaging.Server.Messages.Enums;

namespace TestMessaging.Server.Processors.Implementations
{
    public class UserSocketProcessor : IUserSocketProcessor
    {
        private readonly ILogger _logger;

        private readonly IMessageConsumer<MessageReceivedEventArgs> _messageConsumer;

        private readonly IMessagePublisher<MessageReceivedEventArgs> _messagePublisher;

        private readonly IDictionary<MessageType, IMessageProcessor> _messageProcessors;

        private WebSocket _userSocket;

        private string _token;

        private CancellationTokenSource _cancellationTokenSource;

        private int _isConnected = 0;

        private readonly TokenGenerator _tokenGenerator;

        public UserSocketProcessor(
            IMessageConsumer<MessageReceivedEventArgs> messageConsumer, 
            IMessagePublisher<MessageReceivedEventArgs> messagePublisher, 
            IEnumerable<IMessageProcessor> messageProcessors,
            TokenGenerator tokenGenerator,
            ILogger<UserSocketProcessor> logger)
        {
            _tokenGenerator = tokenGenerator;
            _messageConsumer = messageConsumer;
            _messagePublisher = messagePublisher;
            _logger = logger;
            _messageProcessors = messageProcessors.ToDictionary(x => x.MessageType, y => y);
            _messageConsumer.NewMessageReceived += OnNewMessageReceived;
        }

        public async Task StartMessageProcessing(WebSocket socket, CancellationToken token)
        {
            if (Interlocked.CompareExchange(ref _isConnected, 1, 0) == 1)
            {
                throw new InvalidOperationException("Message processing is already started");
            }

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            _userSocket = socket;

            var pipe = new Pipe();
            try
            {
                var writing = FillPipeAsync(pipe.Writer, _cancellationTokenSource.Token);
                var reading = ReadPipeAsync(pipe.Reader, _cancellationTokenSource.Token);

                await Task.WhenAll(reading, writing).ConfigureAwait(false);

                var userName = _tokenGenerator.GetUserName(_token);

                _messagePublisher.Publish(new MessageReceivedEventArgs
                {
                    Text = "Leaved chat",
                    TimeStamp = DateTime.UtcNow,
                    UserName = userName
                });
            }
            finally
            {
                _cancellationTokenSource.Cancel();
            }
        }

        private async Task FillPipeAsync(PipeWriter writer, CancellationToken token)
        {
            const int minimumBufferSize = 1024 * 4;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                    var memory = writer.GetMemory(minimumBufferSize);
                    try
                    {
                        var receiveResult = await _userSocket.ReceiveAsync(memory, token).ConfigureAwait(false);
                        if (_userSocket.CloseStatus.HasValue)
                        {
                            break;
                        }

                        writer.Advance(receiveResult.Count);

                        if (receiveResult.EndOfMessage)
                        {
                            var result = await writer.FlushAsync(token).ConfigureAwait(false);
                            if (result.IsCompleted)
                            {
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to get data stream");
                        await _userSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Internal server error, contact administrator", _cancellationTokenSource.Token).ConfigureAwait(false);
                        break;
                    }
                }

                await _userSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Socket was closed", _cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Internal sever error");
                await _userSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Internal server error, contact administrator", _cancellationTokenSource.Token).ConfigureAwait(false);
            }
            finally
            {
                writer.Complete();
            }
        }

        private async Task ReadPipeAsync(PipeReader reader, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                    var result = await reader.ReadAsync(token).ConfigureAwait(false);

                    var buffer = result.Buffer;

                    var message = buffer.First.To<Message>();

                    if (!string.IsNullOrEmpty(message.Token))
                    {
                        if (_tokenGenerator.ValidateToken(message.Token))
                        {
                            await _userSocket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "Invalid token.", _cancellationTokenSource.Token).ConfigureAwait(false);
                            break;
                        }
                        _token = message.Token;
                    }

                    if (_messageProcessors.TryGetValue(message.MessageType, out var messageProcessor))
                    {
                        var response = await messageProcessor.GetResponseAsync(message);
                        if (response != null)
                        {
                            var responseBody = response.GetBytes();
                            await _userSocket.SendAsync(responseBody, WebSocketMessageType.Binary, true, token).ConfigureAwait(false);
                        }
                    }

                    reader.AdvanceTo(buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            finally
            {
                reader.Complete();
            }
        }

        private async void OnNewMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (_userSocket == null)
            {
                _logger.LogWarning("No connected socket for current message");
                return;
            }

            if (!_userSocket.CloseStatus.HasValue)
            {
                var body = e.GetBytes();
                await _userSocket.SendAsync(body, WebSocketMessageType.Binary, true, _cancellationTokenSource.Token).ConfigureAwait(false);
                return;
            }

            _logger.LogInformation("Client socket was closed");
            _cancellationTokenSource?.Cancel();
        }

        public void Dispose()
        {
            _messageConsumer.NewMessageReceived -= OnNewMessageReceived;
            _cancellationTokenSource?.Cancel();
            _userSocket?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}