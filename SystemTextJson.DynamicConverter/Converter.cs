﻿using System;
using System.Diagnostics;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace SystemTextJson.DynamicConverter {
	public class Converter : JsonConverter<dynamic> {
		private static dynamic GetNumberFromReader(ref Utf8JsonReader reader) {
			// Try to read it an integer first.
			// Might as well use the smallest type we can get away with.
			// TODO: perhaps make this behavior configurable via constructor params.
			// TODO: do we need to worry about unsigned types?
			// TODO: do we need to worry about byte/sbyte, short?
			var gotInt = reader.TryGetInt32(out var intVal);
			var gotLong = reader.TryGetInt64(out var longVal);
			var intEqualsLong = gotInt && gotLong && intVal == longVal;
			if (intEqualsLong)
				return intVal;
			if (gotLong)
				return longVal;

			// Must be a real number then.
			// Let's not mess around with floats. We could try those
			// first and compare them with the double result, but we'd find
			// that some values look equal but float<->double comparisons
			// fail due to tiny floating point inconsistencies. For example,
			// 1.1, 2.2, 3.3, and 4.4 are all "inequal" between float and
			// double, but 5.5 is equal.
			return reader.GetDouble();
		}

		private static void ThrowNotEnoughJsonException(JsonTokenType finalTokenType) => throw new JsonException($"Invalid JSON: ended with a {finalTokenType} token.");
		internal static dynamic ReadDynamicJsonObject(ref Utf8JsonReader reader, JsonSerializerOptions options) {
			do {
				var tokenType = reader.TokenType;
				switch (tokenType) {
					case JsonTokenType.StartArray:
						var list = new List<dynamic>();
						while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
							list.Add(ReadDynamicJsonObject(ref reader, options));
						// If we ended with something other than an EndArray, we've run out of JSON.
						if (reader.TokenType != JsonTokenType.EndArray)
							ThrowNotEnoughJsonException(tokenType);
						return list.ToArray();
					case JsonTokenType.StartObject:
						var dynamicObject = new ExpandoObject() as IDictionary<string, object>;
						while (reader.Read() && (tokenType = reader.TokenType) != JsonTokenType.EndObject) {
							// MUST be PropertyName
							Debug.Assert(tokenType == JsonTokenType.PropertyName, $"The token immediately following StartObject *must* be an EndObject or PropertyName, but {tokenType} was encountered.");
							var propertyName = reader.GetString();
							if (reader.Read())
								dynamicObject.Add(propertyName, ReadDynamicJsonObject(ref reader, options));
							else ThrowNotEnoughJsonException(tokenType);
						}
						if (reader.TokenType != JsonTokenType.EndObject)
							ThrowNotEnoughJsonException(tokenType);
						return dynamicObject;
					case JsonTokenType.String:
						// Dates, DateTimeOffsets and TimeSpans are encoded as strings.
						var str = reader.GetString();
						if (!IsBasicNumber(str)) {
							if (reader.TryGetDateTime(out var dateTime))
								return dateTime;
							if (reader.TryGetDateTimeOffset(out var dateTimeOffset))
								return dateTimeOffset;
							if (TimeSpan.TryParse(str, out var timespan))
								return timespan;
						}
						return str;
					case JsonTokenType.Number:
						return GetNumberFromReader(ref reader);
					case JsonTokenType.True:
						return true;
					case JsonTokenType.False:
						return false;
					case JsonTokenType.Null:
						return null;
					// Should never happen.
					case JsonTokenType.EndArray:
					case JsonTokenType.EndObject:
					case JsonTokenType.PropertyName:
						throw new JsonException($"{tokenType} encountered outside of appropriate processing loop.");
					case JsonTokenType.Comment:
					// JSON can have comments? Who knew. Ignore these anyway.
					case JsonTokenType.None:
						break;
				}
			} while (reader.Read());
			throw new JsonException($"No actual data was read.");
		}

		private static bool IsBasicNumber(string str) => long.TryParse(str, out _) || double.TryParse(str, out _);

		public override dynamic Read(
			ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options) => ReadDynamicJsonObject(ref reader, options);

		public override void Write(
			Utf8JsonWriter writer,
			dynamic value,
			JsonSerializerOptions options) {
			// Remove this converter to prevent recursion.
			// Standard serializer has no problem writing dynamics.
			var limitedOptions = new JsonSerializerOptions(options);
			limitedOptions.Converters.Remove(this);
			JsonSerializer.Serialize(writer, value, limitedOptions);
		}
	}
}