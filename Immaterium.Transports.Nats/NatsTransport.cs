using System;
using System.Threading.Tasks;
using NATS.Client;
using NATS.Client.JetStream;

namespace Immaterium.Transports.Nats
{
    public class NatsTransport : IImmateriumTransport
    {
        private readonly IConnection _connection;

        private readonly IJetStream _jetStream;

        public NatsTransport()
        {
            _connection = new ConnectionFactory().CreateConnection("nats://localhost:4222");
            _jetStream = _connection.CreateJetStreamContext();
        }

        public NatsTransport(string url)
        {
            _connection = new ConnectionFactory().CreateConnection(url);
            _jetStream = _connection.CreateJetStreamContext();
        }

        public NatsTransport(IConnection connection)
        {
            _connection = connection;
            _jetStream = _connection.CreateJetStreamContext();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        public event EventHandler<ImmateriumMessage> OnMessage;

        public void Listen(string serviceName, bool exclusive = false)
        {

            try
            {
                var info = StreamConfiguration.Builder()
                    .WithName(serviceName)
                    .WithSubjects(serviceName)
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

            sub = _jetStream.PushSubscribeAsync(serviceName, serviceName, OnReceive, true,
                PushSubscribeOptions.Builder()
                    .WithStream(serviceName)
                    .WithDurable(serviceName)
                    .Build());

            sub.Start();
        }

        private IJetStreamPushAsyncSubscription sub;
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
                immateriumMessage.Headers.TryAdd(headerKey, headerValue);
            }

            OnMessage?.Invoke(this, immateriumMessage);
        }

        public void Stop()
        {
        }

        public async Task Send(ImmateriumMessage message)
        {
            await _jetStream.PublishAsync(message.Receiver, message.Body, PublishOptions.Builder()
                .WithStream(message.Receiver)
                .Build());
        }

        public Task<ImmateriumMessage> Post(ImmateriumMessage message)
        {
            throw new NotImplementedException();
        }

        public Task Publish(ImmateriumMessage message)
        {
            throw new NotImplementedException();
        }

        public void Subscribe(string targetServiceName, Subscriber<ImmateriumMessage> action, bool durable = true)
        {
            throw new NotImplementedException();
        }
    }
}
