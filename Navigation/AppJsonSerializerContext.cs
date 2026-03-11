using System.Collections.Generic;
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
// 必须加上这行，以支持 API 直接返回 IEnumerable<FastDataResult>
[JsonSerializable(typeof(IEnumerable<FastDataResult>))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(GuidResponse))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}