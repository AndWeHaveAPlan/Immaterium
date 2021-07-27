namespace Immaterium
{
    public interface IImmateriumSerializer
    {
        byte[] Serialize(object obj);

        T Deserialize<T>(byte[] bytes);

        ImmateriumTransportMessage CreateMessage(object obj);
    }
}