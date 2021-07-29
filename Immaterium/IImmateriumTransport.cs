using System;
using System.Threading.Tasks;

namespace Immaterium
{
    public interface IImmateriumTransport : IDisposable
    {
        event EventHandler<ImmateriumMessage> OnMessage;

        void Listen(string serviceName, bool exclusive = false);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task Send(ImmateriumMessage message);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<ImmateriumMessage> Post(ImmateriumMessage message);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task Publish(ImmateriumMessage message);

        /// <summary> 
        /// </summary>
        /// <param name="targetServiceName"></param>
        /// <param name="action"></param>
        /// <param name="durable"></param>
        void Subscribe(string targetServiceName, Subscriber<ImmateriumMessage> action, bool durable = true);
    }

    /*
    public class ImmateriumMessage
    {
        public ImmateriumHeaderCollection Headers { get; set; }

        public ImmateriumMessage()
        {
            Headers = new ImmateriumHeaderCollection();
        }

        public ImmateriumMessage(ImmateriumHeaderCollection header)
        {
            Headers = header;
        }

        public byte[] Body;
    }*/
}