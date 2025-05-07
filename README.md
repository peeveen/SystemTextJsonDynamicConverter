# System.Text.Json Dynamic Converter

Deserialize data into a `dynamic` type when using `System.Text.Json`.

- .NET Standard 2.0 compatible
- [Available from nuget.org](https://www.nuget.org/packages/SystemTextJson.DynamicConverter)

## Usage

```csharp
class MyClass {

	... etc ...

	[JsonInclude]
	[System.Text.Json.Serialization.JsonConverter(typeof(SystemTextJson.DynamicConverter.Converter))]
	public dynamic MyDynamicData { get; set; }

	[JsonInclude]
	[System.Text.Json.Serialization.JsonConverter(typeof(SystemTextJson.DynamicConverter.CollectionConverter))]
	public dynamic[] MyDynamicDataArray { get; set; }

	... etc ...

}

var result = JsonSerializer.Deserialize<MyClass>(json);
var val = result.MyDynamicData.some._dynamic.property.somewhere;
```

Alternatively ...

```csharp
services.AddControllers().AddJsonOptions(options =>
	options.JsonSerializerOptions.Converters.Add(new SystemTextJson.DynamicConverter.Converter())
);
```
