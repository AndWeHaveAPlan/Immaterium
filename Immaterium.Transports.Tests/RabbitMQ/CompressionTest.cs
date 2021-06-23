using System.Text;
using Immaterium.Transports.RabbitMQ;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Immaterium.Transports.Tests.RabbitMQ
{
    [TestClass]
    public class CompressionTest
    {

        [TestMethod]
        public void Compression()
        {
            var compressor = new GzipCompressor();

            var str = "rqx345ta4fd3w6tgse5tds5cetvg45t gst4w t eth .u o5a tyh,!gsafdtaftdasfdada";

            var bytes = Encoding.UTF8.GetBytes(str);

            bytes = compressor.Decompress(compressor.Compress(bytes));

            Assert.AreEqual(str, Encoding.UTF8.GetString(bytes));

        }

    }
}
