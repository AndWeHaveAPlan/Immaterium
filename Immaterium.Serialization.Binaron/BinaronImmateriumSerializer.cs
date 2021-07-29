using System.IO;
using System.Text;
using Binaron.Serializer;

namespace Immaterium.Serialization.Binaron
{
    public class BinaronImmateriumSerializer : IImmateriumSerializer
    {
        public byte[] Serialize(object obj)
        {
            var stream = new MemoryStream();
            BinaronConvert.Serialize(obj, stream);

            return stream.ToArray();
        }

        public T Deserialize<T>(byte[] bytes)
        {
            var stream = new MemoryStream(bytes);

            var result2 = default(T);

            //var result = BinaronConvert.Deserialize<T>(stream);

            BinaronConvert.Populate(result2, stream);

            return result2;
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