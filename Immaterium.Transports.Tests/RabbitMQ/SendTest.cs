using System;
using System.Threading.Tasks;
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
            var factory = new ConnectionFactory() { };
            var connection = factory.CreateConnection();
            return new RabbitMqTransport(connection) { UseCompression = true };
        }

        private static ImmateruimClient CreateClient(string serviceName)
        {
            return new ImmateruimClient(serviceName, CreateTransport());
        }

        [TestMethod]
        [Timeout(50000)]
        public void Send()
        {
            var tcs = new TaskCompletionSource<bool>();


            var server = CreateClient("crow0");
            var client = CreateClient("client0");
            server.Listen(false);

            server.OnMessage += (sender, e) =>
            {
                Assert.IsTrue(ArrayHelper.ByteArrayEqual(ArrayHelper.TestArray1, e.Message.Body));
                tcs.SetResult(true);
            };

            client.Send("crow0", ArrayHelper.TestArray1);

            tcs.Task.Wait();
        }

        [TestMethod]
        [Timeout(500000)]
        public void Post()
        {
            var server = CreateClient("crow1");
            var client = CreateClient("client1");

            server.Listen(false);

            server.OnMessage += (sender, e) =>
            {
                Assert.IsTrue(ArrayHelper.ByteArrayEqual(ArrayHelper.TestArray1, e.Message.Body));
                var reply = server.CreateReply(e.Message, ArrayHelper.TestArray2);

                server.SendRaw(reply);
            };

            var response = client.Post("crow1", ArrayHelper.TestArray1).Result;

            Assert.IsFalse(response == null);
            Assert.IsTrue(ArrayHelper.ByteArrayEqual(response, ArrayHelper.TestArray2));
        }



        [TestMethod]
        [Timeout(5000)]
        public void MultiSend()
        {
            var tcs = new TaskCompletionSource<bool>();


            var server1 = CreateClient("crow2");
            var server2 = CreateClient("crow2");

            var client = CreateClient("client2");

            var c = 0;

            object lockObj = new object();

            server1.Listen(false);
            server1.OnMessage += (sender, e) =>
            {
                Assert.IsTrue(ArrayHelper.ByteArrayEqual(ArrayHelper.TestArray1, e.Message.Body));
                lock (lockObj)
                {
                    c++;
                    if (c >= 10)
                        tcs.SetResult(true);
                }
            };

            server2.Listen(false);
            server2.OnMessage += (sender, e) =>
            {
                Assert.IsTrue(ArrayHelper.ByteArrayEqual(ArrayHelper.TestArray1, e.Message.Body));
                lock (lockObj)
                {
                    c++;
                    if (c >= 10)
                        tcs.SetResult(true);
                }
            };

            for (int i = 0; i < 10; i++)
            {
                client.Send("crow2", ArrayHelper.TestArray1);
            }

            tcs.Task.Wait();
            Assert.AreEqual(10, c);
        }

        /*
        [TestMethod]
        [Timeout(5000)]
        public void MultiPost()
        {

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
        }*/
    }
}
