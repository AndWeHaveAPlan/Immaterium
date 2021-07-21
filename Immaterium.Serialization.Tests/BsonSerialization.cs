using Immaterium.Serialization.Bson;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Immaterium.Serialization.Tests
{
    [TestClass]
    public class BsonSerialization
    {
        [TestMethod]
        public void CreateMessage()
        {
            var serializer = new BsonImmateriumSerializer();

            var message = serializer.CreateMessage("pump-u-rum");

            var deserialized = serializer.Deserialize<string>(message.Body);

            Assert.AreEqual("pump-u-rum", deserialized);
        }

        [TestMethod]
        public void ValueSerialization()
        {
            var serializer = new BsonImmateriumSerializer();

            var bytes = serializer.Serialize(12);
            var result = serializer.Deserialize<int>(bytes);

            Assert.AreEqual(12, result);
        }

        [TestMethod]
        public void StringSerialization()
        {
            var serializer = new BsonImmateriumSerializer();

            var bytes = serializer.Serialize("pickle-pee");
            var result = serializer.Deserialize<object>(bytes);

            Assert.AreEqual("pickle-pee", result);
        }

        [TestMethod]
        public void SimpleSerialization()
        {
            var serializer = new BsonImmateriumSerializer();

            var so = new SimpleClass();
            so.StrField = "pickle-pee";

            var bytes = serializer.Serialize(so);
            var result = serializer.Deserialize<SimpleClass>(bytes);

            Assert.AreEqual(so.StrField, result.StrField);
        }

        [TestMethod]
        public void PropertySerialization()
        {
            var serializer = new BsonImmateriumSerializer();

            var so = new SimpleClass();
            so.StrProp = "pickle-pee";

            var bytes = serializer.Serialize(so);
            var result = serializer.Deserialize<SimpleClass>(bytes);

            Assert.AreEqual(so.StrProp, result.StrProp);
        }

        [TestMethod]
        public void ArrayTest()
        {
            var serializer = new BsonImmateriumSerializer();

            var so = new SimpleClass();
            so.StrProp = "pickle-pee";

            var array = new object[3];

            array[0] = so;
            array[1] = 1;
            array[2] = "asdf";

            var bytes = serializer.Serialize(array);
            var result = serializer.Deserialize<object[]>(bytes);

            Assert.AreEqual(((SimpleClass)array[0]).StrProp, ((SimpleClass)result[0]).StrProp);
        }

        [TestMethod]
        public void StrangeTest()
        {
            var serializer = new BsonImmateriumSerializer();

            var so = new SimpleClass();
            so.StrProp = "pickle-pee";

            var array = new object[3];

            array[0] = so;
            array[1] = 1;
            array[2] = "asdf";

            var bytes = serializer.Serialize(array);
            var obj = serializer.Deserialize<object>(bytes);

            var result = obj as object[];

            Assert.AreEqual(((SimpleClass)array[0]).StrProp, ((SimpleClass)result[0]).StrProp);
        }
    }
}
