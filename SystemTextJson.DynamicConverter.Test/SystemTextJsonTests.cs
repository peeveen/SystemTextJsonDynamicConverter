using System.Text.Json;
using System.Text.Json.Serialization;

namespace SystemTextJson.DynamicConverter.Test;

[TestClass]
public class SystemTextJsonTests {
	private const string QueryString = "?arg=1&arg=2&flam=blee&thing=";

	public static async Task<string> ReadTextFile(string filename) {
		return await File.ReadAllTextAsync(Path.Join("..", "..", "..", "json", filename));
	}

	public static async Task<T?> DeserializeJson<T>(string filename, Func<string, T?> deserializeFunc) {
		return deserializeFunc(await ReadTextFile(filename));
	}

	// Class to deserialize, including dynamic data.
	class TestClass {
		[JsonInclude]
		[JsonPropertyName("property1")]
		public string? Property1 { get; set; }
		[JsonInclude]
		[JsonPropertyName("dynamicData")]
		[JsonConverter(typeof(Converter))]
		public dynamic? DynamicData { get; set; }
		[JsonInclude]
		[JsonPropertyName("property2")]
		public string? Property2 { get; set; }
	}

	private static void AssertArraysMatch<T>(ICollection<T> array1, ICollection<T> array2) {
		var array1Length = array1.Count;
		var array2Length = array2.Count;
		Assert.AreEqual(array1Length, array2Length);
		for (int f = 0; f < array1Length; ++f) {
			var element1 = array1.ElementAt(f);
			var element2 = array2.ElementAt(f);
			Assert.AreEqual(element1, element2);
		}
	}

	private static void AssertObjectArraysMatch<T>(ICollection<T> array1, ICollection<T> array2) {
		var array1Length = array1.Count;
		var array2Length = array2.Count;
		Assert.AreEqual(array1Length, array2Length);
		for (int f = 0; f < array1Length; ++f) {
			var element1 = array1.ElementAt(f);
			var element2 = array2.ElementAt(f);
			if (element1 != null && element2 != null) {
				var props1 = element1.GetType().GetProperties();
				var dictionary = element2 as IDictionary<string, dynamic>;
				var props2 = dictionary?.Keys;
				var propCount1 = props1.Length;
				var propCount2 = props2?.Count;
				Assert.AreEqual(propCount1, propCount2);
				for (int g = 0; g < props1.Length; ++g) {
					var prop1 = props1[g];
					var prop2 = props2?.ElementAt(g);
					Assert.AreEqual(prop1.Name, prop2);
					var value1 = prop1.GetValue(element1);
					var value2 = dictionary?[prop2!];
					Assert.AreEqual(value1, value2);
				}
			}
		}
	}

	private void TestDeserializedObjectData(dynamic result) {
		Assert.IsNotNull(result);
		Assert.AreEqual(true, result.booleanTrueTest);
		Assert.AreEqual(false, result.booleanFalseTest);
		Assert.AreEqual(1234, result.integerTest);
		Assert.AreEqual(123.4, result.floatTest);
		Assert.AreEqual("abcd", result.stringTest);
		Assert.AreEqual("100", result.numericStringTest);
		Assert.AreEqual(null, result.nullTest as object);
		Assert.AreEqual(new DateTime(2023, 04, 09), result.date);
		Assert.AreEqual(new DateTime(2023, 04, 09, 01, 23, 45), result.dateTime);
		Assert.AreEqual(new DateTime(2023, 04, 09, 00, 23, 45), result.dateTimeOffset);
		Assert.AreEqual(new TimeSpan(0, 0, 2, 23, 453, 983), result.timespan);
		var x = Equals(1, result.integerArrayTest[0]);
		AssertArraysMatch(new object[] { 1, 2, 3, 4 }, result.integerArrayTest);
		for (int f = 0; f < result.floatArrayTest.Length; ++f) {
			Assert.IsTrue(result.floatArrayTest[f].GetType() == typeof(float) || result.floatArrayTest[f].GetType() == typeof(double));
			Assert.IsTrue(1.1 * f - result.floatArrayTest[f] <= 0.000001);
		}
		AssertArraysMatch(new object[] { "a", "b", "c", "d" }, result.stringArrayTest);
		AssertArraysMatch(new object[] { true, false, true, false }, result.booleanArrayTest);
		AssertObjectArraysMatch(new[] { new { property = "thing1" }, new { property = "thing2" }, new { property = "thing3" }, new { property = "thing4" } }, result.objectArrayTest);
		AssertArraysMatch(new object[] { 1, 2, 3, 4 }, result.nestedArrayTest[0]);
		for (int f = 0; f < result.nestedArrayTest[1].Length; ++f) {
			Assert.IsTrue(result.nestedArrayTest[1][f].GetType() == typeof(float) || result.nestedArrayTest[1][f].GetType() == typeof(double));
			Assert.IsTrue(1.1 * f - result.nestedArrayTest[1][f] <= 0.000001);
		}
		AssertArraysMatch(new object[] { "a", "b", "c", "d" }, result.nestedArrayTest[2]);
		AssertArraysMatch(new object[] { true, false, true, false }, result.nestedArrayTest[3]);
		AssertObjectArraysMatch(new[] { new { property = "thing1" }, new { property = "thing2" }, new { property = "thing3" }, new { property = "thing4" } }, result.nestedArrayTest[4]);
	}

	public class TestObject {
		public TestObject(IReadOnlyCollection<string> strings, dynamic dynamicData) {
			Strings = strings;
			DynamicData = dynamicData;
		}
		public IReadOnlyCollection<string> Strings { get; set; }
		public dynamic DynamicData { get; set; }
	}

	[TestMethod]
	public void TestSerialization() {
		var dynamicData = new { UserName = "PC BIL", Tenant = "BIL Enterprises", Group = "Management", Level = 10 };
		var testObject = new TestObject(["hello", "goodbye"], dynamicData);
		var serializationOptions = new JsonSerializerOptions();
		serializationOptions.Converters.Add(new Converter());
		var serializedJson = JsonSerializer.Serialize(testObject, serializationOptions);
		var deserializedDescriptor = JsonSerializer.Deserialize<TestObject>(serializedJson, serializationOptions);
		Assert.IsNotNull(deserializedDescriptor);
	}

	[TestMethod]
	public async Task TestDeserialization() {
		var result = await DeserializeJson("test.json", json => JsonSerializer.Deserialize<TestClass>(json));
		Assert.IsNotNull(result);
		Assert.AreEqual("something", result.Property1);
		Assert.AreEqual("somethingElse", result.Property2);
		TestDeserializedObjectData(result.DynamicData);
		TestDeserializedObjectData(result.DynamicData?.objectTest);
		TestDeserializedObjectData(result.DynamicData?.objectTest?.nestedObjectTest);
	}

	[TestMethod]
	public void TestArraySerialization() {
		var dynamicData = new { UserName = "PC BIL", Tenant = "BIL Enterprises", Group = "Management", Level = 10 };
		var testObject = new dynamic[] { new TestObject(["hello", "goodbye"], dynamicData), dynamicData };
		var serializationOptions = new JsonSerializerOptions();
		serializationOptions.Converters.Add(new Converter());
		serializationOptions.Converters.Add(new CollectionConverter());
		var serializedJson = JsonSerializer.Serialize(testObject, serializationOptions);
		var deserializedArray = JsonSerializer.Deserialize<dynamic[]>(serializedJson, serializationOptions);
		Assert.IsNotNull(deserializedArray);
		Assert.AreEqual(2, deserializedArray.Length);
		Assert.AreEqual(deserializedArray[0].Strings[0], "hello");
		Assert.AreEqual(deserializedArray[1].Tenant, "BIL Enterprises");
	}
}
