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
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        Console.WriteLine($"FastDB 数据库文件路径: {dbPath}");

        // 建议在连接字符串中加入 Pooling=False (如果不使用并发池) 或者保持默认
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var cmd = _connection.CreateCommand();
        // 优化 1: 启用 WAL (Write-Ahead Logging) 提升并发和写入性能
        cmd.CommandText = """
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;
            PRAGMA temp_store = MEMORY;

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
        // 优化 2: 预估初始容量，减少扩容开销
        var sb = new StringBuilder(1024);
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

            // 优化 3: 使用 Span 避免 TrimStart 产生新的字符串分配，并兼容 JSON 数组 '['
            var contentSpan = content.AsSpan().TrimStart();
            bool isJson = contentSpan.Length > 0 && (contentSpan[0] == '{' || contentSpan[0] == '[');
            var contentJson = isJson ? content : JsonSerializer.Serialize(content, AppJsonSerializerContext.Default.String);

            sb.Append($"{{\"id\":\"{id}\",\"content\":{contentJson},\"hashKey\":\"{hKey}\",\"createTime\":\"{createTime}\",\"updateTime\":{updateTime}}}");
        }
        sb.Append(']');
        return sb.ToString();
    }

    public List<FastData> GetByHashKey(string hashKey)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Content, HashKey, CreateTime, UpdateTime FROM FastData WHERE HashKey = @hashKey ORDER BY CreateTime DESC";
        cmd.Parameters.AddWithValue("@hashKey", hashKey);
        return ReadAll(cmd);
    }

    public FastData? GetByIdOnly(string id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Content, HashKey, CreateTime, UpdateTime FROM FastData WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        return ReadOne(cmd);
    }

    public FastData? GetById(string id, string hashKey)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Content, HashKey, CreateTime, UpdateTime FROM FastData WHERE Id = @id AND HashKey = @hashKey";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@hashKey", hashKey);
        return ReadOne(cmd);
    }

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

    public bool Delete(string id, string hashKey)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM FastData WHERE Id = @id AND HashKey = @hashKey";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@hashKey", hashKey);

        return cmd.ExecuteNonQuery() > 0;
    }

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

    public int Clear(string hashKey)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM FastData WHERE HashKey = @hashKey";
        cmd.Parameters.AddWithValue("@hashKey", hashKey);
        return cmd.ExecuteNonQuery();
    }

    public List<FastData> Search(string hashKey, JsonElement jsonQuery)
    {
        var all = GetByHashKey(hashKey);
        var results = new List<FastData>(all.Count / 4); // 适度预估容量

        foreach (var item in all)
        {
            // 优化 4: 提前拦截明显非 JSON 的格式，避免 try-catch 的高昂开销
            var contentSpan = item.Content.AsSpan().TrimStart();
            if (contentSpan.Length == 0 || (contentSpan[0] != '{' && contentSpan[0] != '['))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(item.Content);
                if (JsonContains(doc.RootElement, jsonQuery))
                {
                    results.Add(item);
                }
            }
            catch (JsonException)
            {
                // Content 不是有效 JSON，跳过
            }
        }

        return results;
    }

    private static bool JsonContains(JsonElement source, JsonElement query)
    {
        if (query.ValueKind != source.ValueKind)
        {
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
                    if (!source.TryGetProperty(prop.Name, out var sourceValue) || !JsonContains(sourceValue, prop.Value))
                        return false;
                }
                return true;

            case JsonValueKind.Array:
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
        // 优化 5: 零分配 MD5 哈希计算 (.NET 7/8/9)
        int maxByteCount = Encoding.UTF8.GetMaxByteCount(input.Length);

        // 阈值设为 1024 字节，超出则使用 ArrayPool (或者直接 new)，避免爆栈
        if (maxByteCount <= 1024)
        {
            Span<byte> utf8Bytes = stackalloc byte[maxByteCount];
            int bytesWritten = Encoding.UTF8.GetBytes(input, utf8Bytes);

            Span<byte> hashBytes = stackalloc byte[16]; // MD5 固定 16 字节
            MD5.HashData(utf8Bytes[..bytesWritten], hashBytes);
            return Convert.ToHexStringLower(hashBytes);
        }
        else
        {
            // 对于超长字符串，退化为分配数组
            var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexStringLower(bytes);
        }
    }

    public void Dispose()
    {
        // 确保主动清理状态
        if (_connection.State == System.Data.ConnectionState.Open)
        {
            _connection.Close();
        }
        _connection.Dispose();
    }
}