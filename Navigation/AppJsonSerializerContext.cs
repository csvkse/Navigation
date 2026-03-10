using System.Text.Json;
using System.Text.Json.Serialization;

namespace Navigation;

// ===== AOT JSON 序列化上下文 =====

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(FastData))]
[JsonSerializable(typeof(FastData[]))]
[JsonSerializable(typeof(FastDataResult))]
[JsonSerializable(typeof(FastDataResult[]))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
