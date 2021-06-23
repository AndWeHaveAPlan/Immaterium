using System.Collections.Generic;

namespace Immaterium.Transports.RabbitMQ
{
    public static class DictionaryExtensions
    {
        public static object TryGetValue(this IDictionary<string, object> dictionary, string key)
        {
            return dictionary.TryGetValue(key, out object value) ? value : null;
        }
    }
}
