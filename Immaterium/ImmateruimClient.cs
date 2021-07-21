using System;
using System.Threading.Tasks;

namespace Immaterium
{
    public class MessageReceivedEventArgs
    {
        public ImmateriumMessage Message { get; internal set; }
        //public object BodyObject { get; internal set; }
        //public byte[] Body => Message.Body;
    }

    /// <summary>
    /// 
    /// </summary>
    public class ImmateruimClient
    {
        private readonly string _serviceName;
        private readonly IImmateriumSerializer _serializer;
        private readonly IImmateriumTransport _transport;

        public event EventHandler<MessageReceivedEventArgs> OnMessage;

        public ImmateruimClient(string serviceName, IImmateriumSerializer serializer, IImmateriumTransport transport)
        {
            _serviceName = serviceName;
            _serializer = serializer;
            _transport = transport;

            _transport.OnMessage += (sender, transportMessage) =>
            {
                switch (transportMessage.Headers.Type)
                {
                    case ImmateriumMessageType.Common:
                    case ImmateriumMessageType.Response:
                    case ImmateriumMessageType.Request:
                        OnMessage?.Invoke(
                            this,
                            new MessageReceivedEventArgs
                            {
                                Message = new ImmateriumMessage(transportMessage.Headers)
                                {
                                    Body = _serializer.Deserialize<object>(transportMessage.Body)
                                }
                            });
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
            _transport.Send(ToTransportMessage(messageToSend));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public ImmateriumMessage CreateReply(ImmateriumMessage request, object data = null)
        {
            var response = request.CreateReply();
            if (data != null)
                response.Body = data;

            //response.CorrelationId = request.CorrelationId;
            //response.Receiver = request.ReplyTo;
            //response.Sender = _serviceName;
            //response.Type = ImmateriumMessageType.Response;

            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestArgs"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public ImmateriumMessage CreateReply(MessageReceivedEventArgs requestArgs, object data = null)
        {
            return CreateReply(requestArgs.Message, data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="to"></param>
        /// <param name="objectToSend"></param>
        /// <param name="headers"></param>
        public void Send(string to, object objectToSend, params (string name, string value)[] headers)
        {
            var immateriumTransportMessage = _serializer.CreateMessage(objectToSend);
            immateriumTransportMessage.Headers.Receiver = to;
            immateriumTransportMessage.Headers.Sender = _serviceName;
            immateriumTransportMessage.Headers.Type = ImmateriumMessageType.Common;

            foreach (var (name, value) in headers)
            {
                immateriumTransportMessage.Headers.TryAdd(name, value);
            }

            _transport.Send(immateriumTransportMessage);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<T> Post<T>(string to, object objectToSend, params (string name, string value)[] headers) where T : class
        {
            var immateriumMessage = new ImmateriumMessage();// _serializer.CreateMessage(objectToSend);

            immateriumMessage.Body = objectToSend;

            immateriumMessage.Headers.Receiver = to;
            immateriumMessage.Headers.Sender = _serviceName;
            immateriumMessage.Headers.Type = ImmateriumMessageType.Request;

            foreach (var (name, value) in headers)
            {
                immateriumMessage.Headers.TryAdd(name, value);
            }

            var response = await PostRaw(immateriumMessage);
            var result = response.Body as T;

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageToSend"></param>
        /// <returns></returns>
        public async Task<ImmateriumMessage> PostRaw(ImmateriumMessage messageToSend)
        {
            var t = await _transport.Post(ToTransportMessage(messageToSend));

            return FromTransportMessage(t);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <param name="headers"></param>
        public async void Publish(object body, params (string name, string value)[] headers)
        {
            var eventMessage = new ImmateriumMessage();// _serializer.CreateMessage(body);

            eventMessage.Body = body;

            eventMessage.Headers.Type = ImmateriumMessageType.Event;

            foreach (var (name, value) in headers)
            {
                eventMessage.Headers.TryAdd(name, value);
            }

            await _transport.Publish(ToTransportMessage(eventMessage));
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
                new Subscriber<ImmateriumTransportMessage>(message =>
                {
                    // TODO: try/catch
                    T obj = _serializer.Deserialize<T>(message.Body);
                    subscriber.Invoke(obj);
                })
                , durable);
        }

        private ImmateriumTransportMessage ToTransportMessage(ImmateriumMessage originalMessage)
        {
            return new ImmateriumTransportMessage(originalMessage.Headers)
            {
                Body = _serializer.Serialize(originalMessage.Body)
            };
        }

        private ImmateriumMessage FromTransportMessage(ImmateriumTransportMessage transportMessage)
        {
            return new ImmateriumMessage(transportMessage.Headers)
            {
                Body = _serializer.Deserialize<object>(transportMessage.Body)
            };
        }
    }
}
