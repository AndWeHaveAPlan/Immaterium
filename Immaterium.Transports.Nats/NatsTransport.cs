using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using NATS.Client;
using NATS.Client.JetStream;

namespace Immaterium.Transports.Nats
{
    public class NatsTransport : IImmateriumTransport
    {
        private readonly IConnection _connection;

        private readonly IJetStream _jetStream;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<ImmateriumMessage>> _replyTcs =
            new ConcurrentDictionary<string, TaskCompletionSource<ImmateriumMessage>>();

        private static readonly Random Rng = new Random();

        private string _serviceName;
        private string _replyQueueName;

        private readonly List<IJetStreamPushAsyncSubscription> _subscriptions =
            new List<IJetStreamPushAsyncSubscription>();

        /// <summary>
        /// Creates transport with defult settings (localhost:4222)
        /// </summary>
        public NatsTransport()
        {
            _connection = new ConnectionFactory().CreateConnection("nats://localhost:4222");
            _jetStream = _connection.CreateJetStreamContext();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        public NatsTransport(string url)
        {
            _connection = new ConnectionFactory().CreateConnection(url);
            _jetStream = _connection.CreateJetStreamContext();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public NatsTransport(IConnection connection)
        {
            _connection = connection;
            _jetStream = _connection.CreateJetStreamContext();
        }

        #region IImmateriumMessage

        public event EventHandler<ImmateriumMessage> OnMessage;

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Unsubscribe();
                subscription.Dispose();
            }
            _connection.Dispose();
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="exclusive"></param>
        public void Listen(string serviceName, bool exclusive = false)
        {
            _serviceName = serviceName;

            try
            {
                var info = StreamConfiguration.Builder()
                    .WithName(_serviceName)
                    .WithSubjects($"{_serviceName}.messages.>")
                    //.WithReplicas(3)
                    .WithStorageType(StorageType.Memory)
                    .Build();

                _connection.CreateJetStreamManagementContext().AddStream(info);
            }
            catch (NATSJetStreamException e)
            {
                if (e.ApiErrorCode != 10058)
                    throw;
            }

            var directSubscription = _jetStream.PushSubscribeAsync($"{_serviceName}.messages.direct", $"{_serviceName}-direct", OnReceive, true,
                PushSubscribeOptions.Builder()
                    .WithStream(_serviceName)
                    .WithDurable($"{_serviceName}-direct")
                    .Build());

            directSubscription.Start();
            _subscriptions.Add(directSubscription);
        }



        /// <summary>
        /// 
        /// </summary>
        public void ListenReply()
        {
            _replyQueueName = $"{_serviceName}.messages.reply.{Guid.NewGuid()}";

            var replySubscription = _jetStream.PushSubscribeAsync(_replyQueueName, $"{_serviceName}-reply", OnReceive, true,
                PushSubscribeOptions.Builder()
                    .WithStream(_serviceName)
                    .Build());

            replySubscription.Start();
            _subscriptions.Add(replySubscription);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnReceive(object sender, MsgHandlerEventArgs args)
        {
            var message = args.Message;
            var immateriumMessage = new ImmateriumMessage()
            {
                Body = message.Data
            };

            foreach (string headerKey in message.Header.Keys)
            {
                var headerValue = message.Header[headerKey];
                immateriumMessage.Headers.Set(headerKey, headerValue);
            }

            if (immateriumMessage.Headers.Type == ImmateriumMessageType.Response
                &&
                _replyTcs.TryGetValue(immateriumMessage.Headers.CorrelationId, out var tcs))
            {
                tcs.SetResult(immateriumMessage);
                _replyTcs.TryRemove(immateriumMessage.Headers.CorrelationId, out _);
            }
            else
            {
                OnMessage?.Invoke(this, immateriumMessage);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="immateriumMessage"></param>
        /// <returns></returns>
        public async Task Send(ImmateriumMessage immateriumMessage)
        {
            var natsMessage = new Msg
            {
                Data = immateriumMessage.Body,
            };

            switch (immateriumMessage.Type)
            {
                case ImmateriumMessageType.Request:
                case ImmateriumMessageType.Common:
                    natsMessage.Subject = $"{immateriumMessage.Receiver}.messages.direct";
                    break;
                case ImmateriumMessageType.Response:
                    natsMessage.Subject = $"{immateriumMessage.ReplyTo}";
                    break;
                case ImmateriumMessageType.Event:
                    natsMessage.Subject = $"{immateriumMessage.Receiver}.events";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            foreach (KeyValuePair<string, string> header in immateriumMessage.Headers)
            {
                natsMessage.Header.Add(header.Key, header.Value);
            }

            await _jetStream.PublishAsync(natsMessage, PublishOptions.Builder()
                .WithStream(immateriumMessage.Receiver)
                .Build());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="immateriumMessage"></param>
        /// <returns></returns>
        public Task<ImmateriumMessage> Post(ImmateriumMessage immateriumMessage)
        {
            if (_replyQueueName == null)
            {
                ListenReply();
            }

            immateriumMessage.Headers.CorrelationId = CreateCorrelationId();
            immateriumMessage.Headers.Type = ImmateriumMessageType.Request;
            immateriumMessage.Headers.ReplyTo = _replyQueueName;

            var tcs = new TaskCompletionSource<ImmateriumMessage>();
            _replyTcs.TryAdd(immateriumMessage.Headers.CorrelationId, tcs);

            _ = Send(immateriumMessage);

            return tcs.Task;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task Publish(ImmateriumMessage message)
        {
            //ssage.
            await Send(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetServiceName"></param>
        /// <param name="action"></param>
        /// <param name="durable"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Subscribe(string targetServiceName, Subscriber<ImmateriumMessage> action, bool durable = true)
        {
            throw new NotImplementedException();
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string CreateCorrelationId()
        {
            return Rng.Next(10_000, 100_000).ToString();
        }
    }
}
