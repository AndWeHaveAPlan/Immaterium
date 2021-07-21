using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Immaterium.Serialization.Bson
{
    public class BsonImmateriumSerializer : IImmateriumSerializer
    {
        public byte[] Serialize(object obj)
        {
            var w = new Wrapper<object>(obj);
            var bytes = w.ToBson();
            return bytes;
        }

        public T Deserialize<T>(byte[] bytes)
        {
            var result = BsonSerializer.Deserialize<Wrapper<T>>(bytes);
            return result.Value;
        }

        public ImmateriumTransportMessage CreateMessage(object obj)
        {
            var bytes = Serialize(obj);

            var result = new ImmateriumTransportMessage
            {
                Body = bytes
            };

            return result;
        }

        private class Wrapper<T>
        {
            public Wrapper(T val)
            {
                Value = val;
            }

            [BsonElement] public T Value;
        }
    }
}
