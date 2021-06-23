using System.IO;
using System.IO.Compression;

namespace Immaterium.Transports.RabbitMQ
{
    public class GzipCompressor
    {
        public byte[] Compress(byte[] input)
        {
            using var inputStream = new MemoryStream(input);
            using var outputStream = new MemoryStream();
            using (var gs = new GZipStream(outputStream, CompressionMode.Compress))
            {
                inputStream.CopyTo(gs);
            }

            return outputStream.ToArray();
        }

        public byte[] Decompress(byte[] input)
        {
            using var inputStream = new MemoryStream(input);
            using var outputStream = new MemoryStream();
            using (var gs = new GZipStream(inputStream, CompressionMode.Decompress))
            {
                gs.CopyTo(outputStream);
            }

            return outputStream.ToArray();
        }
    }
}
