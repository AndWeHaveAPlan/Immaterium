using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Immaterium.Transports.RabbitMQ;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;

namespace Immaterium.Transports.Tests.RabbitMQ
{
    [TestClass]
    public class EventsTest
    {
        public static byte[] TestArray = { 6, 5, 4, 3, 2 };

        private static RabbitMqTransport CreateTransport()
        {
            var factory = new ConnectionFactory();
            var connection = factory.CreateConnection();
            return new RabbitMqTransport(connection);
        }

        private static ImmateruimClient CreateClient(string serviceName)
        {
            return new ImmateruimClient(serviceName, CreateTransport());
        }

        [TestMethod]
        [Timeout(10000)]
        public void BasicPublish()
        {
            var tcs = new TaskCompletionSource<bool>();

            var server = CreateClient("crow");
            var client = CreateClient("client");

            var sub = new Subscriber(msg =>
              {
                  Assert.IsTrue(ArrayHelper.ByteArrayEqual(ArrayHelper.TestArray1, msg.Body));
                  tcs.SetResult(true);
              });
            //Assert.
            server.Listen();

            client.Subscribe("crow", sub);

            server.Publish(ArrayHelper.TestArray1);

            //client.Send("crow", "pickle-pee");

            tcs.Task.Wait();
        }

    }
}
