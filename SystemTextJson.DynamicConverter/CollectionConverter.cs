using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SystemTextJson.DynamicConverter {
	public class CollectionConverter : JsonConverter<dynamic[]> {
		public static readonly CollectionConverter Instance = new CollectionConverter();

		public override dynamic[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			if (reader.TokenType != JsonTokenType.StartArray) {
				reader.Skip();
				return null;
			}
			return Converter.ReadDynamicJsonArray(ref reader, options);
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
