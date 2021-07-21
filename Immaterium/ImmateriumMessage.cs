namespace Immaterium
{
    /// <summary>
    /// 
    /// </summary>
    public class ImmateriumMessage
    {
        /// <summary>
        /// Get or set CorrelationId header
        /// </summary>
        public string CorrelationId
        {
            get => Headers.CorrelationId;
            set => Headers.CorrelationId = value;
        }

        /// <summary>
        /// Get or set Type header
        /// </summary>
        public ImmateriumMessageType Type
        {
            get => Headers.Type;
            set => Headers.Type = value;
        }

        /// <summary>
        /// Sender service name (from)
        /// </summary>
        public string Sender
        {
            get => Headers.Sender;
            set => Headers.Sender = value;
        }

        /// <summary>
        /// Receiver service name (to)
        /// </summary>
        public string Receiver
        {
            get => Headers.Receiver;
            set => Headers.Receiver = value;
        }

        /// <summary>
        /// Reply address
        /// </summary>
        public string ReplyTo
        {
            get => Headers.ReplyTo;
            set => Headers.ReplyTo = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public ImmateriumHeaderCollection Headers { get; } = new ImmateriumHeaderCollection();

        /// <summary>
        /// Message payload
        /// </summary>
        public virtual object Body { get; set; }

        public ImmateriumMessage()
        {
            Body = null;
            Sender = "";
            Receiver = "";
            Type = ImmateriumMessageType.Common;
        }

        /// <summary>
        /// 
        /// </summary>
        public ImmateriumMessage(ImmateriumHeaderCollection headers)
        {
            Body = null;
            Headers = headers;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ImmateriumMessage CreateReply()
        {
            var response = new ImmateriumMessage
            {
                CorrelationId = CorrelationId,
                Receiver = ReplyTo,
                Sender = Receiver,
                Type = ImmateriumMessageType.Response
            };

            return response;
        }
    }
}