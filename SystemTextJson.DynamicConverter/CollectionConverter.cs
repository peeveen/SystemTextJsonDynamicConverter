using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SystemTextJson.DynamicConverter {
	public class CollectionConverter : JsonConverter<dynamic[]> {
		public override dynamic[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			if (reader.TokenType != JsonTokenType.StartArray) {
				reader.Skip();
				return null;
			}
			var list = new List<dynamic>();
			while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
				var item = Converter.ReadDynamicJsonObject(ref reader, options);
				list.Add(item);
			}
			return list.ToArray();
		}

		public override void Write(Utf8JsonWriter writer, dynamic[] value, JsonSerializerOptions options) {
			// Remove this converter to prevent recursion.
			var limitedOptions = new JsonSerializerOptions(options);
			limitedOptions.Converters.Remove(this);
			writer.WriteStartArray();
			foreach (dynamic item in value)
				// Standard serializer has no problem writing dynamics.
				JsonSerializer.Serialize(writer, item, limitedOptions);
			writer.WriteEndArray();
		}
	}
}
