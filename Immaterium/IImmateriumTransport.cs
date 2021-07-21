using System;
using System.Threading.Tasks;

namespace Immaterium
{
    public interface IImmateriumTransport
    {
        event EventHandler<ImmateriumTransportMessage> OnMessage;

        void Listen(string serviceName, bool exclusive = false);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        Task Send(ImmateriumTransportMessage message);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        Task<ImmateriumTransportMessage> Post(ImmateriumTransportMessage message);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        Task Publish(ImmateriumTransportMessage message);

        /// <summary> 
        /// </summary>
        /// <param name="targetServiceName"></param>
        /// <param name="action"></param>
        /// <param name="durable"></param>
        void Subscribe(string targetServiceName, Subscriber<ImmateriumTransportMessage> action, bool durable = true);
    }

    public class ImmateriumTransportMessage
    {
        public ImmateriumHeaderCollection Headers { get; set; }

        public ImmateriumTransportMessage()
        {
            Headers = new ImmateriumHeaderCollection();
        }

        public ImmateriumTransportMessage(ImmateriumHeaderCollection header)
        {
            Headers = header;
        }

        public byte[] Body;
    }
}