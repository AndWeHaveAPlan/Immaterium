using System.Threading.Tasks;
using Immaterium.Serialization.Bson;
using Immaterium.Transports.RabbitMQ;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;

namespace Immaterium.Transports.Tests.RabbitMQ
{
    [TestClass]
    public class EventsTest
    {
        private static RabbitMqTransport CreateTransport()
        {
            var factory = new ConnectionFactory();
            var connection = factory.CreateConnection();
            return new RabbitMqTransport(connection);
        }

        private static ImmateruimClient CreateClient(string serviceName)
        {
            return new ImmateruimClient(serviceName, new BsonImmateriumSerializer(), CreateTransport());
        }

        [TestMethod]
        [Timeout(500000)]
        public void BasicPublish()
        {
            var tcs = new TaskCompletionSource<bool>();

            var server = CreateClient("crow");
            var client = CreateClient("client");

            var sub = new Subscriber<string>(str =>
              {
                  Assert.AreEqual(str, "ololo");
                  tcs.SetResult(true);
              });

            server.Listen();

            client.Subscribe("crow", sub);

            server.Publish("ololo");

            //client.Send("crow", "pickle-pee");

            tcs.Task.Wait();
        }

    }
}
