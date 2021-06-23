using System;
using System.Threading.Tasks;

namespace Immaterium
{
    /// <summary>
    /// 
    /// </summary>
    public class ImmateruimClient
    {
        private readonly string _serviceName;
        private readonly IImmateriumSerializer _serializer;
        private readonly IImmateriumTransport _transport;

        public event EventHandler<ImmateriumMessage> OnMessage;

        public ImmateruimClient(string serviceName, IImmateriumSerializer serializer, IImmateriumTransport transport)
        {
            _serviceName = serviceName;
            _serializer = serializer;
            _transport = transport;

            _transport.OnMessage += (sender, message) =>
            {
                switch (message.Type)
                {
                    case ImmateriumMessageType.Common:
                    case ImmateriumMessageType.Response:
                    case ImmateriumMessageType.Request:
                        OnMessage?.Invoke(this, message);
                        break;
                    case ImmateriumMessageType.Event:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exclusive"></param>
        public void Listen(bool exclusive = false)
        {
            _transport.Listen(_serviceName, exclusive);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageToSend"></param>
        public void SendRaw(ImmateriumMessage messageToSend)
        {
            _transport.Send(messageToSend);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public ImmateriumMessage CreateReply(ImmateriumMessage request, object data = null)
        {
            var response = data != null
                ? _serializer.CreateMessage(data)
                : new ImmateriumMessage();

            response.CorrelationId = request.CorrelationId;
            response.Receiver = request.ReplyTo;
            //response.Sender = _serviceName;
            response.Type = ImmateriumMessageType.Response;

            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="to"></param>
        /// <param name="objectToSend"></param>
        /// <param name="headers"></param>
        public void Send(string to, object objectToSend, params (string name, string value)[] headers)
        {
            var immateriumMessage = _serializer.CreateMessage(objectToSend);
            immateriumMessage.Receiver = to;
            immateriumMessage.Sender = _serviceName;
            immateriumMessage.Type = ImmateriumMessageType.Common;

            foreach (var (name, value) in headers)
            {
                immateriumMessage.Headers.TryAdd(name, value);
            }

            _transport.Send(immateriumMessage);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<T> Post<T>(string to, object objectToSend, params (string name, string value)[] headers) where T : class
        {
            var immateriumMessage = _serializer.CreateMessage(objectToSend);
            immateriumMessage.Receiver = to;
            immateriumMessage.Sender = _serviceName;
            immateriumMessage.Type = ImmateriumMessageType.Request;

            foreach (var (name, value) in headers)
            {
                immateriumMessage.Headers.TryAdd(name, value);
            }

            var response = await PostRaw(immateriumMessage);
            var result = _serializer.Deserialize<T>(response.Body);

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageToSend"></param>
        /// <returns></returns>
        public Task<ImmateriumMessage> PostRaw(ImmateriumMessage messageToSend)
        {
            var t = _transport.Post(messageToSend);

            return t;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <param name="headers"></param>
        public async void Publish(object body, params (string name, string value)[] headers)
        {
            var eventMessage = _serializer.CreateMessage(body);
            eventMessage.Type = ImmateriumMessageType.Event;

            foreach (var (name, value) in headers)
            {
                eventMessage.Headers.TryAdd(name, value);
            }

            await _transport.Publish(eventMessage);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetServiceName"></param>
        /// <param name="subscriber"></param>
        /// <param name="durable"></param>
        public void Subscribe<T>(string targetServiceName, Subscriber<T> subscriber, bool durable = true)
        {
            _transport.Subscribe(
                targetServiceName,
                new Subscriber<ImmateriumMessage>(message =>
                {
                    T obj = _serializer.Deserialize<T>(message.Body);
                    subscriber.Invoke(obj);
                })
                , durable);
        }
    }
}
