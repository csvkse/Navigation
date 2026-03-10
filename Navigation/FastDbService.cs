using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Navigation;

/// <summary>
/// FastDB SQLite 数据访问服务 (AOT 兼容)
/// </summary>
public class FastDbService : IDisposable
{
    private readonly SqliteConnection _connection;

    public FastDbService(string dbPath = "fastdb.db")
    {
        // 确保数据库文件所在目录存在
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        Console.WriteLine($"FastDB 数据库文件路径: {dbPath}");
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS FastData (
                Id TEXT PRIMARY KEY,
                Content TEXT NOT NULL DEFAULT '{}',
                HashKey TEXT NOT NULL,
                CreateTime TEXT NOT NULL,
                UpdateTime TEXT
            );
            CREATE INDEX IF NOT EXISTS IX_FastData_HashKey ON FastData(HashKey);
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 按 HashKey 获取所有数据，直接拼接为 JSON 数组字符串，避免 DTO 序列化开销
    /// </summary>
    public string GetByHashKeyRawJson(string hashKey)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Content, HashKey, CreateTime, UpdateTime FROM FastData WHERE HashKey = @hashKey ORDER BY CreateTime DESC";
        cmd.Parameters.AddWithValue("@hashKey", hashKey);

        using var reader = cmd.ExecuteReader();
        var sb = new StringBuilder();
        sb.Append('[');
        bool first = true;
        
        while (reader.Read())
        {
            if (!first) sb.Append(',');
            first = false;

            var id = reader.GetString(0);
            var content = reader.GetString(1);
            var hKey = reader.GetString(2);
            var createTime = reader.GetString(3);
            var updateTime = reader.IsDBNull(4) ? "null" : $"\"{reader.GetString(4)}\"";

            // If content is already a valid JSON object string (starts with '{'), we embed it directly
            // Otherwise, we wrap it in a JSON string (for fallback safety)
            var contentJson = content.TrimStart().StartsWith('{') ? content : JsonSerializer.Serialize(content);

            sb.Append($"{{\"id\":\"{id}\",\"content\":{contentJson},\"hashKey\":\"{hKey}\",\"createTime\":\"{createTime}\",\"updateTime\":{updateTime}}}");
        }
        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>
    /// 按 HashKey 获取所有数据，按 CreateTime 降序
    /// </summary>
    public List<FastData> GetByHashKey(string hashKey)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Content, HashKey, CreateTime, UpdateTime FROM FastData WHERE HashKey = @hashKey ORDER BY CreateTime DESC";
        cmd.Parameters.AddWithValue("@hashKey", hashKey);

        return ReadAll(cmd);
    }

    /// <summary>
    /// 仅按 Id 获取单条数据（用于 ReadOnly 端点）
    /// </summary>
    public FastData? GetByIdOnly(string id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Content, HashKey, CreateTime, UpdateTime FROM FastData WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        return ReadOne(cmd);
    }

    /// <summary>
    /// 按 Id + HashKey 获取单条数据
    /// </summary>
    public FastData? GetById(string id, string hashKey)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Content, HashKey, CreateTime, UpdateTime FROM FastData WHERE Id = @id AND HashKey = @hashKey";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@hashKey", hashKey);

        return ReadOne(cmd);
    }

    /// <summary>
    /// 插入数据
    /// </summary>
    public void Insert(FastData entity)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO FastData (Id, Content, HashKey, CreateTime, UpdateTime) VALUES (@id, @content, @hashKey, @createTime, @updateTime)";
        cmd.Parameters.AddWithValue("@id", entity.Id);
        cmd.Parameters.AddWithValue("@content", entity.Content);
        cmd.Parameters.AddWithValue("@hashKey", entity.HashKey);
        cmd.Parameters.AddWithValue("@createTime", entity.CreateTime);
        cmd.Parameters.AddWithValue("@updateTime", (object?)entity.UpdateTime ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 批量插入数据
    /// </summary>
    public void InsertBulk(IEnumerable<FastData> entities)
    {
        using var transaction = _connection.BeginTransaction();
        using var cmd = _connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = "INSERT INTO FastData (Id, Content, HashKey, CreateTime, UpdateTime) VALUES (@id, @content, @hashKey, @createTime, @updateTime)";
        
        var pId = cmd.Parameters.Add("@id", SqliteType.Text);
        var pContent = cmd.Parameters.Add("@content", SqliteType.Text);
        var pHashKey = cmd.Parameters.Add("@hashKey", SqliteType.Text);
        var pCreateTime = cmd.Parameters.Add("@createTime", SqliteType.Text);
        var pUpdateTime = cmd.Parameters.Add("@updateTime", SqliteType.Text);

        foreach (var entity in entities)
        {
            pId.Value = entity.Id;
            pContent.Value = entity.Content;
            pHashKey.Value = entity.HashKey;
            pCreateTime.Value = entity.CreateTime;
            pUpdateTime.Value = (object?)entity.UpdateTime ?? DBNull.Value;
            cmd.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    /// <summary>
    /// 更新 Content 和 UpdateTime
    /// </summary>
    public bool Update(string id, string hashKey, string content, string updateTime)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE FastData SET Content = @content, UpdateTime = @updateTime WHERE Id = @id AND HashKey = @hashKey";
        cmd.Parameters.AddWithValue("@content", content);
        cmd.Parameters.AddWithValue("@updateTime", updateTime);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@hashKey", hashKey);

        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 批量更新 Content 和 UpdateTime
    /// </summary>
    public int UpdateBulk(string hashKey, IEnumerable<(string Id, string Content)> items)
    {
        using var transaction = _connection.BeginTransaction();
        using var cmd = _connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = "UPDATE FastData SET Content = @content, UpdateTime = @updateTime WHERE Id = @id AND HashKey = @hashKey";

        var pId = cmd.Parameters.Add("@id", SqliteType.Text);
        var pContent = cmd.Parameters.Add("@content", SqliteType.Text);
        var pUpdateTime = cmd.Parameters.Add("@updateTime", SqliteType.Text);
        var pHashKey = cmd.Parameters.Add("@hashKey", SqliteType.Text);

        var now = DateTime.Now.ToString("o");
        int count = 0;

        foreach (var (id, content) in items)
        {
            pId.Value = id;
            pContent.Value = content;
            pUpdateTime.Value = now;
            pHashKey.Value = hashKey;
            count += cmd.ExecuteNonQuery();
        }

        transaction.Commit();
        return count;
    }

    /// <summary>
    /// 删除数据
    /// </summary>
    public bool Delete(string id, string hashKey)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM FastData WHERE Id = @id AND HashKey = @hashKey";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@hashKey", hashKey);

        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 批量删除数据
    /// </summary>
    public int DeleteBulk(string hashKey, IEnumerable<string> ids)
    {
        using var transaction = _connection.BeginTransaction();
        using var cmd = _connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = "DELETE FROM FastData WHERE Id = @id AND HashKey = @hashKey";

        var pId = cmd.Parameters.Add("@id", SqliteType.Text);
        var pHashKey = cmd.Parameters.Add("@hashKey", SqliteType.Text);

        int count = 0;
        foreach (var id in ids)
        {
            pId.Value = id;
            pHashKey.Value = hashKey;
            count += cmd.ExecuteNonQuery();
        }

        transaction.Commit();
        return count;
    }

    /// <summary>
    /// 删除某个 HashKey 下的所有数据
    /// </summary>
    public int Clear(string hashKey)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM FastData WHERE HashKey = @hashKey";
        cmd.Parameters.AddWithValue("@hashKey", hashKey);

        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// JSON 搜索：查出 HashKey 下所有数据后，在应用层做 JSON containment 匹配
    /// </summary>
    public List<FastData> Search(string hashKey, JsonElement jsonQuery)
    {
        var all = GetByHashKey(hashKey);
        var results = new List<FastData>();

        foreach (var item in all)
        {
            try
            {
                using var doc = JsonDocument.Parse(item.Content);
                if (JsonContains(doc.RootElement, jsonQuery))
                {
                    results.Add(item);
                }
            }
            catch
            {
                // Content 不是有效 JSON，跳过
            }
        }

        return results;
    }

    /// <summary>
    /// 递归检查 source 是否包含 query 中的所有键值对
    /// 模拟 PostgreSQL 的 @> (containment) 操作
    /// </summary>
    private static bool JsonContains(JsonElement source, JsonElement query)
    {
        if (query.ValueKind != source.ValueKind)
        {
            // 特殊处理：数字比较（int vs double）
            if (query.ValueKind == JsonValueKind.Number && source.ValueKind == JsonValueKind.Number)
            {
                return query.GetDecimal() == source.GetDecimal();
            }
            return false;
        }

        switch (query.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in query.EnumerateObject())
                {
                    if (!source.TryGetProperty(prop.Name, out var sourceValue))
                        return false;
                    if (!JsonContains(sourceValue, prop.Value))
                        return false;
                }
                return true;

            case JsonValueKind.Array:
                // 查询数组中的每个元素必须在源数组中存在
                foreach (var queryItem in query.EnumerateArray())
                {
                    bool found = false;
                    foreach (var sourceItem in source.EnumerateArray())
                    {
                        if (JsonContains(sourceItem, queryItem))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) return false;
                }
                return true;

            case JsonValueKind.String:
                return source.GetString() == query.GetString();

            case JsonValueKind.Number:
                return source.GetDecimal() == query.GetDecimal();

            case JsonValueKind.True:
            case JsonValueKind.False:
                return source.GetBoolean() == query.GetBoolean();

            case JsonValueKind.Null:
                return true;

            default:
                return false;
        }
    }

    #region 内部读取辅助

    private static List<FastData> ReadAll(SqliteCommand cmd)
    {
        var list = new List<FastData>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapRow(reader));
        }
        return list;
    }

    private static FastData? ReadOne(SqliteCommand cmd)
    {
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapRow(reader) : null;
    }

    private static FastData MapRow(SqliteDataReader reader)
    {
        return new FastData
        {
            Id = reader.GetString(0),
            Content = reader.GetString(1),
            HashKey = reader.GetString(2),
            CreateTime = reader.GetString(3),
            UpdateTime = reader.IsDBNull(4) ? null : reader.GetString(4)
        };
    }

    #endregion

    /// <summary>
    /// 计算 MD5 哈希
    /// </summary>
    public static string ComputeMd5(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
