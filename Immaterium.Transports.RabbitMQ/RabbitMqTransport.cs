using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Immaterium.Transports.RabbitMQ
{
    /// <summary>
    /// 
    /// </summary>
    public class RabbitMqTransport : IImmateriumTransport
    {
        private string _serviceName;
        private readonly IModel _model;

        private string _replyQueueName;
        private string _eventsExchangeName;

        public event EventHandler<ImmateriumMessage> OnMessage;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<ImmateriumMessage>> _replyTcs =
            new ConcurrentDictionary<string, TaskCompletionSource<ImmateriumMessage>>();

        public bool UseCompression = false;
        private GzipCompressor _compressor = new GzipCompressor();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rabbitMqConnection"></param>
        public RabbitMqTransport(IConnection rabbitMqConnection)
        {
            _model = rabbitMqConnection.CreateModel();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OnReceive(object sender, BasicDeliverEventArgs message)
        {
            var basicProperties = message.BasicProperties;
            var rawBody = message.Body.Span.ToArray();

            var immateriumMessage = new ImmateriumMessage()
            {
                Body = rawBody
            };

            GetHeaders(basicProperties, immateriumMessage);
            Decompress(immateriumMessage);

            if (
                immateriumMessage.Headers.Type == ImmateriumMessageType.Response
                &&
                _replyTcs.TryGetValue(immateriumMessage.Headers.CorrelationId, out var tcs)
                )
            {
                tcs.SetResult(immateriumMessage);
                _replyTcs.TryRemove(immateriumMessage.Headers.CorrelationId, out _);

                return;
            }

            OnMessage?.Invoke(this, immateriumMessage);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="exclusive"></param>
        public void Listen(string serviceName, bool exclusive = false)
        {
            _serviceName = serviceName;

            _model.ExchangeDeclare(_serviceName, "fanout", durable: true, autoDelete: false);

            if (!exclusive)
            {
                var queue = _model.QueueDeclare(_serviceName, exclusive: false, durable: true, autoDelete: false);
                _model.QueueBind(_serviceName, _serviceName, "");
                var consumer = new EventingBasicConsumer(_model);
                consumer.Received += OnReceive;
                _model.BasicConsume(queue.QueueName, true, consumer);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void ListenReply()
        {
            var replyQueue = _model.QueueDeclare();
            _replyQueueName = replyQueue.QueueName;
            var replyConsumer = new EventingBasicConsumer(_model);
            replyConsumer.Received += OnReceive;
            _model.BasicConsume(_replyQueueName, true, replyConsumer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageToSend"></param>
        public Task Send(ImmateriumMessage messageToSend)
        {
            var bp = _model.CreateBasicProperties();
            bp.Headers = new Dictionary<string, object>();
            bp.CorrelationId = messageToSend.Headers.CorrelationId;
            bp.ReplyTo = messageToSend.Headers.ReplyTo;

            Compress(messageToSend);
            SetHeaders(bp, messageToSend);

            switch (messageToSend.Headers.Type)
            {
                case ImmateriumMessageType.Common:
                    _model.BasicPublish(messageToSend.Headers.Receiver, "", true, bp, messageToSend.Body);
                    break;
                case ImmateriumMessageType.Request:
                    //await PostRaw(messageToSend);
                    _model.BasicPublish(messageToSend.Headers.Receiver, "", true, bp, messageToSend.Body);
                    break;
                case ImmateriumMessageType.Response:
                    _model.BasicPublish("", messageToSend.Headers.Receiver, true, bp, messageToSend.Body);
                    break;
                case ImmateriumMessageType.Event:
                    _model.BasicPublish(_eventsExchangeName, "", true, bp, messageToSend.Body);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageToSend"></param>
        /// <returns></returns>
        public Task<ImmateriumMessage> Post(ImmateriumMessage messageToSend)
        {
            // TODO: message.body null or empty

            if (_replyQueueName == null)
            {
                ListenReply();
            }

            messageToSend.Headers.ReplyTo = _replyQueueName;
            messageToSend.Headers.CorrelationId = CreateCorrelationId();
            messageToSend.Headers.Type = ImmateriumMessageType.Request;

            var tcs = new TaskCompletionSource<ImmateriumMessage>();

            Send(messageToSend);

            _replyTcs.TryAdd(messageToSend.Headers.CorrelationId, tcs);

            return tcs.Task;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetServiceName"></param>
        /// <param name="subscriber"></param>
        /// <param name="durable"></param>
        public void Subscribe(string targetServiceName, Subscriber<ImmateriumMessage> subscriber, bool durable = true)
        {
            var queueName = $"{_serviceName}-{targetServiceName}";
            var targetExchangeName = $"{targetServiceName}-events";
            _model.ExchangeDeclare(targetExchangeName, "fanout", durable: durable, autoDelete: !durable);

            _model.QueueDeclare(queueName, true, false, false);

            _model.QueueBind(queueName, targetExchangeName, "");

            var consumer = new EventingBasicConsumer(_model);
            consumer.Received += (sender, message) =>
            {
                var basicProperties = message.BasicProperties;
                var immateriumMessage = new ImmateriumMessage
                {
                    Body = message.Body.Span.ToArray()
                };
                GetHeaders(basicProperties, immateriumMessage);
                Decompress(immateriumMessage);

                subscriber?.Invoke(immateriumMessage);
            };
            _model.BasicConsume(queueName, true, consumer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="immateriumMessage"></param>
        /// <returns></returns>
        public Task Publish(ImmateriumMessage immateriumMessage)
        {
            EnsureEventExchange();

            immateriumMessage.Headers.Type = ImmateriumMessageType.Event;
            immateriumMessage.Headers.Sender = _serviceName;

            Send(immateriumMessage);

            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        private void EnsureEventExchange()
        {
            if (_eventsExchangeName != null)
                return;

            _eventsExchangeName = $"{_serviceName}-events";
            _model.ExchangeDeclare(_eventsExchangeName, "fanout", true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string BytesToString(object input)
        {
            var bytes = (byte[])input;

            if (bytes == null)
                return null;

            return Encoding.UTF8.GetString(bytes);
        }

        private static readonly Random Rng = new Random();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        private string CreateCorrelationId(int length = 8)
        {
            string str = "";
            for (int i = 0; i < length; i++)
            {
                str += ((char)(Rng.Next(1, 26) + 64)).ToString();
            }

            return str;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="basicProperties"></param>
        /// <param name="immateriumMessage"></param>
        private void SetHeaders(IBasicProperties basicProperties, ImmateriumMessage immateriumMessage)
        {
            //basicProperties.hea
            foreach (var (key, value) in immateriumMessage.Headers)
            {
                basicProperties.Headers.TryAdd(key, value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="basicProperties"></param>
        /// <param name="immateriumMessage"></param>
        private void GetHeaders(IBasicProperties basicProperties, ImmateriumMessage immateriumMessage)
        {
            foreach (var (key, value) in basicProperties.Headers)
            {
                immateriumMessage.Headers[key] = BytesToString(value);
            }

            var messageHeaders = immateriumMessage.Headers;

            messageHeaders.CorrelationId = basicProperties.CorrelationId;
            messageHeaders.ReplyTo = basicProperties.ReplyTo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private void Decompress(ImmateriumMessage message)
        {
            if (message.Headers["Compression"] == "gzip")
            {
                message.Body = _compressor.Decompress(message.Body);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private void Compress(ImmateriumMessage message)
        {
            if (UseCompression == false)
                return;

            if (message.Headers["Compression"] == "gzip")
                return;

            message.Body = _compressor.Compress(message.Body);
            message.Headers["Compression"] = "gzip";
        }

        public void Dispose()
        {
            _model?.Dispose();
        }
    }
}