namespace Immaterium
{
    public interface IImmateriumSerializer
    {
        byte[] Serialize(object obj);

        public T Deserialize<T>(byte[] bytes);

        public ImmateriumMessage CreateMessage(object obj);
    }
}