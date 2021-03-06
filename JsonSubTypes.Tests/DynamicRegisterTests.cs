﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    public class DynamicRegisterTests
    {
        public abstract class Animal
        {
            [JsonProperty("age", Order = 1)]
            public int Age { get; set; }
        }

        public class Dog : Animal
        {
            public bool CanBark { get; set; } = true;
        }

        public class Cat : Animal
        {
            [JsonProperty("catLives", Order = 0)]
            public int Lives { get; set; } = 7;
        }

        public abstract class Fish : Animal
        {
            [JsonProperty("fins", Order = 2)]
            public uint FinCount { get; set; }
        }

        public class Shark : Fish
        {
            [JsonProperty("teethRows", Order = 3)]
            public uint TeethRows { get; set; }
        }

        public class HammerheadShark : Shark
        {
            [JsonProperty("hammerSize", Order = 4)]
            public float HammerSize { get; set; }
        }

        public enum AnimalType
        {
            Dog = 1,
            Cat = 2,
            Shark = 3,
            HammerheadShark = 4
        }

        [Test]
        public void DeserializeTest()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .Build());

            var json = "{\"catLives\":6,\"type\":2,\"age\":11}";

            var result = JsonConvert.DeserializeObject<Animal>(json);


            Assert.AreEqual(typeof(Cat), result.GetType());
            Assert.AreEqual(11, result.Age);
            Assert.AreEqual(6, (result as Cat)?.Lives);
        }

        [Test]
        public void DeserializeIncompleteTest()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .Build());

            var json = "{\"type\":2}";

            var result = JsonConvert.DeserializeObject<Animal>(json);


            Assert.AreEqual(typeof(Cat), result.GetType());
            Assert.AreEqual(0, result.Age);
            Assert.AreEqual(7, (result as Cat)?.Lives);
        }

        [Test]
        public void SerializeTest()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .Build());

            var json = "{\"catLives\":6,\"age\":11,\"type\":2}";

            var result = JsonConvert.SerializeObject(new Cat { Age = 11, Lives = 6 });

            Assert.AreEqual(json, result);
        }

        [Test]
        public void UnregisteredTypeSerializeTest()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .RegisterSubtype(typeof(Shark), AnimalType.Shark)
                .Build());

            var json = "{\"age\":11,\"fins\":4,\"teethRows\":4,\"hammerSize\":42.1}"; // no type property shall be added

            var result = JsonConvert.SerializeObject(new HammerheadShark
            {
                Age = 11,
                FinCount = 4,
                HammerSize = 42.1f,
                TeethRows = 4
            });

            Assert.AreEqual(json, result);
        }


        [Test]
        public void UnregisteredTypeSerializeTest2()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                // Shark is not registered
                .RegisterSubtype(typeof(HammerheadShark), AnimalType.HammerheadShark)
                .Build());

            var json = "{\"age\":11,\"fins\":4,\"teethRows\":4}"; // no type property shall be added

            var result = JsonConvert.SerializeObject(new Shark
            {
                Age = 11,
                FinCount = 4,
                TeethRows = 4
            });

            Assert.AreEqual(json, result);
        }

        [Test]
        public void UnregisteredTypeDeserializeTest()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .RegisterSubtype(typeof(Shark), AnimalType.Shark)
                // HammerheadShark is not registered
                .Build());

            var json = "{\"age\":11,\"fins\":4,\"teethRows\":4,\"hammerSize\":42.1,\"type\":4}";

            var exception = Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<Animal>(json));

            Assert.IsTrue(exception.Message.Contains("Type is an interface or abstract class and cannot be instantiated."));
        }

        [Test]
        public void NestedTypeDeserializeTest()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                // Shark is not registered
                .RegisterSubtype(typeof(HammerheadShark), AnimalType.HammerheadShark)
                .Build());

            var json = "{\"age\":11,\"fins\":3,\"teethRows\":4,\"hammerSize\":42.1,\"type\":4}";

            var result = JsonConvert.DeserializeObject<Animal>(json);


            Assert.AreEqual(typeof(HammerheadShark), result.GetType());
            Assert.AreEqual(11, result.Age);
            Assert.AreEqual(3u, (result as Fish)?.FinCount);
            Assert.AreEqual(4u, (result as Shark)?.TeethRows);
            Assert.AreEqual(42.1f, (result as HammerheadShark)?.HammerSize);
        }

        public interface IExpression
        {

            string Type { get; }
        }

        public class BinaryExpression : IExpression
        {
            public IExpression SubExpressionA { get; set; }
            public IExpression SubExpressionB { get; set; }
            public string Type { get { return "Binary"; } }
        }

        public class ConstantExpression : IExpression
        {
            public string Value { get; set; }
            public string Type { get { return "Constant"; } }
        }

        [Test]
        public void TestIfNestedObjectIsDeserialized()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression), "Type")
                .RegisterSubtype(typeof(ConstantExpression), "Constant")
                .RegisterSubtype(typeof(BinaryExpression), "Binary")
                .Build());

            var binary = JsonConvert.DeserializeObject<IExpression>("{\"Type\":\"Binary\"," +
                                                                    "\"SubExpressionA\":{\"Type\":\"Constant\",\"Value\":\"A\"}," +
                                                                    "\"SubExpressionB\":{\"Type\":\"Constant\",\"Value\":\"B\"}" +
                                                                    "}");

            Assert.AreEqual(typeof(ConstantExpression), (binary as BinaryExpression)?.SubExpressionA.GetType());
        }

        [Test]
        public void TestIfNestedObjectIsSerialized()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression), "Type")
                .RegisterSubtype(typeof(ConstantExpression), "Constant")
                .RegisterSubtype(typeof(BinaryExpression), "Binary")
                .Build());

            var json = JsonConvert.SerializeObject(new BinaryExpression
            {
                SubExpressionA = new ConstantExpression { Value = "A" },
                SubExpressionB = new ConstantExpression { Value = "B" }
            });

            Assert.AreEqual("{" +
                "\"SubExpressionA\":{\"Value\":\"A\",\"Type\":\"Constant\"}," +
                "\"SubExpressionB\":{\"Value\":\"B\",\"Type\":\"Constant\"}" +
                ",\"Type\":\"Binary\"}", json);
        }

        public interface IExpression2
        {

        }

        public class BinaryExpression2 : IExpression2
        {
            public IExpression2 SubExpressionA { get; set; }
            public IExpression2 SubExpressionB { get; set; }
        }

        public class ManyOrExpression2 : IExpression2
        {
            public List<IExpression2> OrExpr { get; set; }
        }

        public class ConstantExpression2 : IExpression2
        {
            public string Value { get; set; }
        }

        [Test]
        public void TestIfNestedObjectIsDeserialized2()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression2), "Type")
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(ConstantExpression2), "Constant")
                .RegisterSubtype(typeof(BinaryExpression2), "Binary")
                .Build());

            var binary = JsonConvert.DeserializeObject<IExpression2>("{\"Type\":\"Binary\"," +
                                                                    "\"SubExpressionA\":{\"Type\":\"Constant\",\"Value\":\"A\"}," +
                                                                    "\"SubExpressionB\":{\"Type\":\"Constant\",\"Value\":\"B\"}" +
                                                                    "}", settings);
            Assert.AreEqual(typeof(ConstantExpression2), (binary as BinaryExpression2)?.SubExpressionA.GetType());
        }

        [Test]
        public void TestIfNestedObjectIsSerialized2()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression2), "Type")
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(ConstantExpression2), "Constant")
                .RegisterSubtype(typeof(BinaryExpression2), "Binary")
                .Build());

            var target = JsonConvert.SerializeObject(new BinaryExpression2
            {
                SubExpressionA = new ConstantExpression2 { Value = "A" },
                SubExpressionB = new ConstantExpression2 { Value = "B" }
            });

            Assert.AreEqual("{" +
                            "\"SubExpressionA\":{\"Value\":\"A\",\"Type\":\"Constant\"}," +
                            "\"SubExpressionB\":{\"Value\":\"B\",\"Type\":\"Constant\"}" +
                            ",\"Type\":\"Binary\"}", target);
        }

        [Test]
        public void TestNestedObjectInBothWay()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression2), "Type")
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(ConstantExpression2), "Constant")
                .RegisterSubtype(typeof(BinaryExpression2), "Binary")
                .RegisterSubtype(typeof(ManyOrExpression2), "ManyOr")
                .Build());

            var target = JsonConvert.SerializeObject(new BinaryExpression2
            {
                SubExpressionA = new ManyOrExpression2 { OrExpr = new List<IExpression2> { new ConstantExpression2 { Value = "A" }, new ConstantExpression2 { Value = "B" } } },
                SubExpressionB = new ManyOrExpression2 { OrExpr = new List<IExpression2> { new ConstantExpression2 { Value = "A" }, new ManyOrExpression2 { OrExpr = new List<IExpression2> { new ConstantExpression2 { Value = "A" }, new ConstantExpression2 { Value = "B" } } } } }
            });

            var json = "{" +
                           "\"SubExpressionA\":{\"OrExpr\":[{\"Value\":\"A\",\"Type\":\"Constant\"},{\"Value\":\"B\",\"Type\":\"Constant\"}],\"Type\":\"ManyOr\"}," +
                           "\"SubExpressionB\":{\"OrExpr\":[{\"Value\":\"A\",\"Type\":\"Constant\"},{\"OrExpr\":[{\"Value\":\"A\",\"Type\":\"Constant\"},{\"Value\":\"B\",\"Type\":\"Constant\"}],\"Type\":\"ManyOr\"}],\"Type\":\"ManyOr\"}" +
                           ",\"Type\":\"Binary\"}";
            Assert.AreEqual(json, target);


            Assert.AreEqual(json, JsonConvert.SerializeObject(JsonConvert.DeserializeObject<IExpression2>(json)));
        }


        [Test]
        public void TestNestedObjectInBothWayParallel()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression2), "Type")
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(ConstantExpression2), "Constant")
                .RegisterSubtype(typeof(BinaryExpression2), "Binary")
                .RegisterSubtype(typeof(ManyOrExpression2), "ManyOr")
                .Build());


            Action test = () => { 
                var target = JsonConvert.SerializeObject(new BinaryExpression2
                {
                    SubExpressionA = new ManyOrExpression2 { OrExpr = new List<IExpression2> { new ConstantExpression2 { Value = "A" }, new ConstantExpression2 { Value = "B" } } },
                    SubExpressionB = new ManyOrExpression2 { OrExpr = new List<IExpression2> { new ConstantExpression2 { Value = "A" }, new ManyOrExpression2 { OrExpr = new List<IExpression2> { new ConstantExpression2 { Value = "A" }, new ConstantExpression2 { Value = "B" } } } } }
                });

                var json = "{" +
                           "\"SubExpressionA\":{\"OrExpr\":[{\"Value\":\"A\",\"Type\":\"Constant\"},{\"Value\":\"B\",\"Type\":\"Constant\"}],\"Type\":\"ManyOr\"}," +
                           "\"SubExpressionB\":{\"OrExpr\":[{\"Value\":\"A\",\"Type\":\"Constant\"},{\"OrExpr\":[{\"Value\":\"A\",\"Type\":\"Constant\"},{\"Value\":\"B\",\"Type\":\"Constant\"}],\"Type\":\"ManyOr\"}],\"Type\":\"ManyOr\"}" +
                           ",\"Type\":\"Binary\"}";
                Assert.AreEqual(json, target);


                Assert.AreEqual(json, JsonConvert.SerializeObject(JsonConvert.DeserializeObject<IExpression2>(json)));
            };

            Parallel.For(0, 100, index => test());
        }
    }
}
