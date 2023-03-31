using System.Collections.Generic;

namespace Immaterium
{
    /// <summary>
    /// 
    /// </summary>
    public class ImmateriumHeaderCollection : Dictionary<string, string>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public new string this[string key]
        {

            get => ContainsKey(key) ? base[key] : null;
            set
            {
                if (ContainsKey(key))
                    base[key] = value;
                else
                    Add(key, value);
            }
        }

        public string CorrelationId
        {
            get => this["CorrelationId"];
            set => this["CorrelationId"] = value;
        }

        /// <summary>
        /// Get or set Type header
        /// </summary>
        public ImmateriumMessageType Type
        {
            get
            {
                if (int.TryParse(this["Type"], out int result))
                {
                    return (ImmateriumMessageType)result;
                }
                return ImmateriumMessageType.Common;
            }
            set => this["Type"] = ((int)value).ToString();
        }

        /// <summary>
        /// Sender service name (from)
        /// </summary>
        public string Sender
        {
            get => this["Sender"];
            set => this["Sender"] = value;
        }

        /// <summary>
        /// Receiver service name (to)
        /// </summary>
        public string Receiver
        {
            get => this["Receiver"];
            set => this["Receiver"] = value;
        }

        /// <summary>
        /// Reply address
        /// </summary>
        public string ReplyTo
        {
            get => this["ReplyTo"];
            set => this["ReplyTo"] = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="headers"></param>
        public void Add(params (string key, string value)[] headers)
        {
            foreach (var (key, value) in headers)
            {
                Add(key, value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="header"></param>
        public bool TryAdd((string key, string value) header)
        {
            if (ContainsKey(header.key))
                return false;

            Add(header.key, header.value);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryAdd(string key, string value)
        {
            if (ContainsKey(key))
                return false;

            Add(key, value);
            return true;
        }
    }
}