using Immaterium.Serialization.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Immaterium.Serialization.Tests
{
    [TestClass]
    public class JsonSerialization
    {

        [TestMethod]
        public void CreateMessage()
        {
            var serializer = new JsonImmateriumSerializer();

            var message = serializer.CreateMessage("pump-u-rum");

            var deserialized = serializer.Deserialize<string>(message.Body);

            Assert.AreEqual("pump-u-rum", deserialized);
        }

        [TestMethod]
        public void ValueSerialization()
        {
            var serializer = new JsonImmateriumSerializer();

            var bytes = serializer.Serialize(12);
            var result = serializer.Deserialize<int>(bytes);

            Assert.AreEqual(12, result);
        }

        [TestMethod]
        public void StringSerialization()
        {
            var serializer = new JsonImmateriumSerializer();

            var bytes = serializer.Serialize("pickle-pee");
            var result = serializer.Deserialize<string>(bytes);

            Assert.AreEqual("pickle-pee", result);
        }

        [TestMethod]
        public void SimpleSerialization()
        {
            var serializer = new JsonImmateriumSerializer();

            var so = new SimpleClass();
            so.StrField = "pickle-pee";

            var bytes = serializer.Serialize(so);
            var result = serializer.Deserialize<SimpleClass>(bytes);

            Assert.AreEqual(so.StrField, result.StrField);
        }

        [TestMethod]
        public void PropertySerialization()
        {
            var serializer = new JsonImmateriumSerializer();

            var so = new SimpleClass();
            so.StrProp = "pickle-pee";

            var bytes = serializer.Serialize(so);
            var result = serializer.Deserialize<SimpleClass>(bytes);

            Assert.AreEqual(so.StrProp, result.StrProp);
        }
    }
}
