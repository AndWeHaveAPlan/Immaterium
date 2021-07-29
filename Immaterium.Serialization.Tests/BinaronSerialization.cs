using System;
using System.ComponentModel;
using System.Dynamic;
using Binaron.Serializer;
using Immaterium.Serialization.Binaron;
using Immaterium.Serialization.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson.Serialization.Serializers;

namespace Immaterium.Serialization.Tests
{
    [TestClass]
    public class BinaronSerialization
    {

        [TestMethod]
        public void CreateMessage()
        {
            var serializer = new BinaronImmateriumSerializer();

            var message = serializer.CreateMessage("pump-u-rum");

            var deserialized = serializer.Deserialize<string>(message.Body);

            Assert.AreEqual("pump-u-rum", deserialized);
        }

        [TestMethod]
        public void ValueSerialization()
        {
            var serializer = new BinaronImmateriumSerializer();

            var bytes = serializer.Serialize(12);
            var result = serializer.Deserialize<int>(bytes);

            Assert.AreEqual(12, result);
        }

        [TestMethod]
        public void StringSerialization()
        {
            var serializer = new BinaronImmateriumSerializer();

            var bytes = serializer.Serialize("pickle-pee");
            var result = serializer.Deserialize<string>(bytes);

            Assert.AreEqual("pickle-pee", result);
        }

        [TestMethod]
        public void SimpleSerialization()
        {
            var serializer = new BinaronImmateriumSerializer();

            var so = new SimpleClass();
            so.StrField = "pickle-pee";

            var bytes = serializer.Serialize(so);
            var result = serializer.Deserialize<SimpleClass>(bytes);

            Assert.AreEqual(so.StrField, result.StrField);
        }

        [TestMethod]
        public void PropertySerialization()
        {
            var serializer = new BinaronImmateriumSerializer();

            var so = new SimpleClass();
            so.StrProp = "pickle-pee";

            var bytes = serializer.Serialize(so);
            var result = serializer.Deserialize<SimpleClass>(bytes);

            Assert.AreEqual(so.StrProp, result.StrProp);
        }

        [TestMethod]
        public void ArrayTest()
        {
            var serializer = new BinaronImmateriumSerializer();

            var so = new SimpleClass();
            so.StrProp = "pickle-pee";

            var array = new object[3];

            array[0] = so;
            array[1] = 1;
            array[2] = "asdf";

            var bytes = serializer.Serialize(array);
            var result = serializer.Deserialize<object[]>(bytes);

            SimpleClass sc = (SimpleClass)Convert.ChangeType(array[0], typeof(SimpleClass));

            Assert.AreEqual(sc.StrProp, so.StrProp);
        }

        [TestMethod]
        public void StrangeTest()
        {
            var serializer = new BinaronImmateriumSerializer();

            var so = new SimpleClass();
            so.StrProp = "pickle-pee";

            var array = new object[3];

            array[0] = so;
            array[1] = 1;
            array[2] = "asdf";

            var bytes = serializer.Serialize(array);
            var obj = serializer.Deserialize<object>(bytes);

            var result = obj as dynamic[];

            var sfdsdf = (new ExpandableObjectConverter()).ConvertTo(result[0], typeof(SimpleClass));
            //BinaronConvert.
            SimpleClass sc = (SimpleClass)Convert.ChangeType(result[0], typeof(SimpleClass));

            Assert.AreEqual(sc.StrProp, (array[0] as SimpleClass).StrProp);
        }
    }
}
