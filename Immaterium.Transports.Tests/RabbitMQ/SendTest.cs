using System.Threading.Tasks;
using Immaterium.Serialization.Bson;
using Immaterium.Transports.RabbitMQ;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;

namespace Immaterium.Transports.Tests.RabbitMQ
{
    [TestClass]
    public class SendTest
    {
        private static RabbitMqTransport CreateTransport()
        {
            var factory = new ConnectionFactory();
            var connection = factory.CreateConnection();
            return new RabbitMqTransport(connection) { UseCompression = true };
        }

        private static ImmateruimClient CreateClient(string serviceName)
        {
            return new ImmateruimClient(serviceName, new BsonImmateriumSerializer(), CreateTransport());
        }

        [TestMethod]
        [Timeout(50000)]
        public void Send()
        {
            var tcs = new TaskCompletionSource<bool>();

            var serializer = new BsonImmateriumSerializer();

            var server = CreateClient("crow0");
            var client = CreateClient("client0");
            server.Listen(false);

            server.OnMessage += (sender, message) =>
            {
                var obj = (string)message.Message.Body as string;
                Assert.AreEqual("pickle-pee", obj);
                tcs.SetResult(true);
            };

            client.Send("crow0", "pickle-pee");

            tcs.Task.Wait();
        }

        [TestMethod]
        [Timeout(500000)]
        public void Post()
        {
            var serializer = new BsonImmateriumSerializer();

            var server = CreateClient("crow1");
            var client = CreateClient("client1");

            server.Listen(false);

            server.OnMessage += (sender, message) =>
            {
                var obj = message.Message.Body as string;
                Assert.AreEqual("pickle-pee", obj);
                var reply = server.CreateReply(message, "pump-u-rum");

                server.SendRaw(reply);
            };

            var response = client.Post<string>("crow1", "pickle-pee").Result;

            Assert.IsFalse(string.IsNullOrWhiteSpace(response));
            Assert.AreEqual("pump-u-rum", response);
        }

        [TestMethod]
        [Timeout(5000)]
        public void MultiSend()
        {
            var tcs = new TaskCompletionSource<bool>();

            //var factory = new ConnectionFactory() { };
            //var factory = new ConnectionFactory() { HostName = "10.133.12.28", UserName = "c2m-bus", Password = "qwerty", VirtualHost = "c2m-bus" };
            //using var connection = factory.CreateConnection();

            var serializer = new BsonImmateriumSerializer();

            var server1 = CreateClient("crow2");
            var server2 = CreateClient("crow2");

            var client = CreateClient("client2");

            var c = 0;

            object lockObj = new object();

            server1.Listen(false);
            server1.OnMessage += (sender, message) =>
            {
                var obj = message.Message.Body as string;
                Assert.AreEqual("pickle-pee", obj);
                lock (lockObj)
                {
                    c++;
                    if (c >= 10)
                        tcs.SetResult(true);
                }
            };

            server2.Listen(false);
            server2.OnMessage += (sender, message) =>
            {
                var obj = message.Message.Body as string;
                Assert.AreEqual("pickle-pee", obj);
                lock (lockObj)
                {
                    c++;
                    if (c >= 10)
                        tcs.SetResult(true);
                }
            };

            for (int i = 0; i < 10; i++)
            {
                client.Send("crow2", "pickle-pee");
            }

            tcs.Task.Wait();
            Assert.AreEqual(10, c);
        }

        [TestMethod]
        [Timeout(5000)]
        public void MultiPost()
        {
            var serializer = new BsonImmateriumSerializer();

            var server = CreateClient("crow3");

            var client1 = CreateClient("client3");
            var client2 = CreateClient("client3");

            server.Listen(false);

            server.OnMessage += (sender, message) =>
            {
                var obj = message.Message.Body as string;
                if (obj == "pickle-pee")
                {
                    var reply = server.CreateReply(message, "pump-u-rum");
                    server.SendRaw(reply);
                }
                else if (obj == "pickle-pee2")
                {
                    var reply = server.CreateReply(message, "pump-u-rum2");
                    server.SendRaw(reply);
                }
            };

            int i;
            for (i = 0; i < 10; i++)
            {
                var response = client1.Post<string>("crow3", "pickle-pee").Result;
                var response2 = client2.Post<string>("crow3", "pickle-pee2").Result;

                Assert.IsFalse(string.IsNullOrWhiteSpace(response));
                Assert.IsFalse(string.IsNullOrWhiteSpace(response2));
                Assert.AreEqual("pump-u-rum", response);
                Assert.AreEqual("pump-u-rum2", response2);
            }

            Assert.AreEqual(10, i);
        }
    }
}
