using System.Text;
using Newtonsoft.Json;

namespace Immaterium.Serialization.Json
{
    public class JsonImmateriumSerializer : IImmateriumSerializer
    {
        public byte[] Serialize(object obj)
        {
            var jsonString = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(jsonString);//.SerializeToUtf8Bytes(obj, new JsonSerializerOptions(){});
        }

        public T Deserialize<T>(byte[] bytes)
        {
            var jsonString = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        public ImmateriumMessage CreateMessage(object obj)
        {
            var bytes = Serialize(obj);

            var result = new ImmateriumMessage
            {
                Body = bytes
            };

            return result;
        }
    }
}
