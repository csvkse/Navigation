namespace Navigation;

/// <summary>
/// FastDB 数据实体
/// </summary>
public class FastData
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = "{}";
    public string HashKey { get; set; } = string.Empty;
    public string CreateTime { get; set; } = string.Empty;
    public string? UpdateTime { get; set; }
}

/// <summary>
/// FastDB API 返回结果
/// </summary>
public class FastDataResult
{
    public string Id { get; set; } = string.Empty;
    public object? Content { get; set; }
    public string HashKey { get; set; } = string.Empty;
    public string CreateTime { get; set; } = string.Empty;
    public string? UpdateTime { get; set; }
}
