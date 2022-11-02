# System.Text.Json Dynamic Converter

Deserialize data into a `dynamic` type when using `System.Text.Json`.

```csharp
class MyClass {

	... etc ...

	[JsonInclude]
	[System.Text.Json.Serialization.JsonConverter(typeof(SystemTextJsonDynamicConverter.DynamicConverter))]
	public dynamic MyDynamicData { get; set; }

	... etc ...

}

var result = JsonSerializer.Deserialize<MyClass>(json)
var val = result.MyDynamicData.some._dynamic.property.somewhere;
```
