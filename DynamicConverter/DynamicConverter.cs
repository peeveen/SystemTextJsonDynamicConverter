using System;
using System.Diagnostics;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace SystemTextJsonDynamicConverter {
	public class DynamicConverter : JsonConverter<dynamic> {
		private dynamic GetNumberFromReader(ref Utf8JsonReader reader) {
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

			// Must be a real number then
			var gotFloat = reader.TryGetSingle(out var floatVal) && !Single.IsInfinity(floatVal);
			var gotDouble = reader.TryGetDouble(out var doubleVal);
			// TODO: do we need to worry about decimal?
			if (gotFloat && gotDouble && floatVal == doubleVal)
				return floatVal;
			// TODO: I would prefer to treat any "Infinity" double results as strings, but calling
			// reader.getString() when it has already parsed the token as a number results in an
			// annoying exception. So I guess we'll just have to go To Infinity And Beyond!
			return doubleVal;
		}

		private void throwNotEnoughJsonException(JsonTokenType finalTokenType) => throw new JsonException($"Invalid JSON: ended with a {finalTokenType} token.");
		private dynamic ReadDynamicJsonObject(ref Utf8JsonReader reader, JsonSerializerOptions options) {
			do {
				var tokenType = reader.TokenType;
				switch (tokenType) {
					case JsonTokenType.StartArray:
						var list = new List<dynamic>();
						if (reader.Read())
							for (; (tokenType = reader.TokenType) != JsonTokenType.EndArray; reader.Read()) {
								var arrayNextToken = reader.TokenType;
								if (arrayNextToken != JsonTokenType.EndArray)
									list.Add(ReadDynamicJsonObject(ref reader, options));
							}
						else throwNotEnoughJsonException(tokenType);
						return list.ToArray();
					case JsonTokenType.StartObject:
						var dynamicObject = new ExpandoObject() as IDictionary<string, object>;
						if (reader.Read())
							for (; (tokenType = reader.TokenType) != JsonTokenType.EndObject; reader.Read()) {
								// MUST be PropertyName
								Debug.Assert(tokenType == JsonTokenType.PropertyName, $"The token immediately following StartObject *must* be an EndObject or PropertyName, but {tokenType} was encountered.");
								var propertyName = reader.GetString();
								if (reader.Read())
									dynamicObject.Add(propertyName, ReadDynamicJsonObject(ref reader, options));
								else throwNotEnoughJsonException(tokenType);
							}
						else throwNotEnoughJsonException(tokenType);
						return dynamicObject;
					case JsonTokenType.String:
						// TODO: do we need to worry about dates etc here?
						return reader.GetString();
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

		public override dynamic Read(
			ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options) {
			return ReadDynamicJsonObject(ref reader, options);
		}

		public override void Write(
			Utf8JsonWriter writer,
			dynamic value,
			JsonSerializerOptions options) {
			JsonSerializer.Serialize(writer, value, options);
		}
	}
}