using System.Text.Json;
using System.Text.Json.Serialization;

namespace DynamicConverter.Test;

[TestClass]
public class Tests {
	public async static Task<string> ReadTextFile(string filename) {
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
		[System.Text.Json.Serialization.JsonConverter(typeof(SystemTextJsonDynamicConverter.DynamicConverter))]
		public dynamic? DynamicData { get; set; }
		[JsonInclude]
		[JsonPropertyName("property2")]
		public string? Property2 { get; set; }
	}

	private void AssertArraysMatch<T>(ICollection<T> array1, ICollection<T> array2) {
		var array1Length = array1.Count;
		var array2Length = array2.Count;
		Assert.AreEqual(array1Length, array2Length);
		for (int f = 0; f < array1Length; ++f) {
			var element1 = array1.ElementAt(f);
			var element2 = array2.ElementAt(f);
			Assert.AreEqual(element1, element2);
		}
	}

	private void AssertObjectArraysMatch<T>(ICollection<T> array1, ICollection<T> array2) {
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
		Assert.AreEqual(null, result.nullTest);
		var x = Object.Equals(1, result.numberArrayTest[0]);
		AssertArraysMatch(new object[] { 1, 2, 3, 4 }, result.numberArrayTest);
		AssertArraysMatch(new object[] { "a", "b", "c", "d" }, result.stringArrayTest);
		AssertArraysMatch(new object[] { true, false, true, false }, result.booleanArrayTest);
		AssertObjectArraysMatch(new[] { new { property = "thing1" }, new { property = "thing2" }, new { property = "thing3" }, new { property = "thing4" } }, result.objectArrayTest);
		AssertArraysMatch(new object[] { 1, 2, 3, 4 }, result.nestedArrayTest[0]);
		AssertArraysMatch(new object[] { "a", "b", "c", "d" }, result.nestedArrayTest[1]);
		AssertArraysMatch(new object[] { true, false, true, false }, result.nestedArrayTest[2]);
		AssertObjectArraysMatch(new[] { new { property = "thing1" }, new { property = "thing2" }, new { property = "thing3" }, new { property = "thing4" } }, result.nestedArrayTest[3]);
	}

	[TestMethod]
	public async Task TestDeserialize() {
		var result = await DeserializeJson<TestClass>("test.json", json => JsonSerializer.Deserialize<TestClass>(json));
		Assert.IsNotNull(result);
		Assert.AreEqual("something", result.Property1);
		Assert.AreEqual("somethingElse", result.Property2);
		TestDeserializedObjectData(result.DynamicData);
		TestDeserializedObjectData(result.DynamicData?.objectTest);
		TestDeserializedObjectData(result.DynamicData?.objectTest?.nestedObjectTest);
	}
}
