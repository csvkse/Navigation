using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Navigation;

/// <summary>
/// FastDB SQLite 数据访问服务 (AOT 兼容，极限性能 + 高并发安全版)
/// </summary>
public class FastDbService
{
    private readonly string _connectionString;

    public FastDbService(string dbPath = "fastdb.db")
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        Console.WriteLine($"FastDB 数据库文件路径: {dbPath}");

        // 并发优化 1: Cache=Shared 共享内存，Default Timeout=5 开启写入排队等待，避免 database is locked
        _connectionString = $"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate;Default Timeout=5;";
        
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        // 并发优化 2: 使用短连接，依托内置连接池，保证绝对的线程安全
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        // 开启 WAL 模式提升并发读写性能
        cmd.CommandText = """
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;
            PRAGMA temp_store = MEMORY;
            PRAGMA busy_timeout = 5000;

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
    /// 按 HashKey 获取所有数据，直接生成 JSON 数组字符串，避免 DTO 序列化开销
    /// </summary>
    public string GetByHashKeyRawJson(string hashKey)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Content, HashKey, CreateTime, UpdateTime FROM FastData WHERE HashKey = @hashKey ORDER BY CreateTime DESC";
        cmd.Parameters.AddWithValue("@hashKey", hashKey);

        using var reader = cmd.ExecuteReader();
        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms); // 零分配高安全 JSON 写入

        writer.WriteStartArray();

        while (reader.Read())
        {
            writer.WriteStartObject();
            writer.WriteString("id", reader.GetString(0));

            var content = reader.GetString(1);
            var contentSpan = content.AsSpan().TrimStart();
            bool isJson = contentSpan.Length > 0 && (contentSpan[0] == '{' || contentSpan[0] == '[');

            if (isJson)
            {
                writer.WritePropertyName("content");
                writer.WriteRawValue(content); 
            }
            else
            {
                writer.WriteString("content", content); 
            }

            writer.WriteString("hashKey", reader.GetString(2));
            writer.WriteString("createTime", reader.GetString(3));
            
            if (reader.IsDBNull(4))
                writer.WriteNull("updateTime");
            else
                writer.WriteString("updateTime", reader.GetString(4));
                
            writer.WriteEndObject();
        }
        
        writer.WriteEndArray();
        writer.Flush();

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public List<FastData> GetByHashKey(string hashKey)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Content, HashKey, CreateTime, UpdateTime FROM FastData WHERE HashKey = @hashKey ORDER BY CreateTime DESC";
        cmd.Parameters.AddWithValue("@hashKey", hashKey);
        
        return ReadAll(cmd);
    }

    public FastData? GetByIdOnly(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Content, HashKey, CreateTime, UpdateTime FROM FastData WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        
        return ReadOne(cmd);
    }

    public FastData? GetById(string id, string hashKey)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Content, HashKey, CreateTime, UpdateTime FROM FastData WHERE Id = @id AND HashKey = @hashKey";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@hashKey", hashKey);
        
        return ReadOne(cmd);
    }

    public void Insert(FastData entity)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
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
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = "INSERT INTO FastData (Id, Content, HashKey, CreateTime, UpdateTime) VALUES (@id, @content, @hashKey, @createTime, @updateTime)";

        var pId = cmd.Parameters.Add("@id", SqliteType.Text);
        var pContent = cmd.Parameters.Add("@content", SqliteType.Text);
        var pHashKey = cmd.Parameters.Add("@hashKey", SqliteType.Text);
        var pCreateTime = cmd.Parameters.Add("@createTime", SqliteType.Text);
        var pUpdateTime = cmd.Parameters.Add("@updateTime", SqliteType.Text);

        cmd.Prepare(); // 预编译提升性能

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
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE FastData SET Content = @content, UpdateTime = @updateTime WHERE Id = @id AND HashKey = @hashKey";
        cmd.Parameters.AddWithValue("@content", content);
        cmd.Parameters.AddWithValue("@updateTime", updateTime);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@hashKey", hashKey);

        return cmd.ExecuteNonQuery() > 0;
    }

    public int UpdateBulk(string hashKey, IEnumerable<(string Id, string Content)> items)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = "UPDATE FastData SET Content = @content, UpdateTime = @updateTime WHERE Id = @id AND HashKey = @hashKey";

        var pId = cmd.Parameters.Add("@id", SqliteType.Text);
        var pContent = cmd.Parameters.Add("@content", SqliteType.Text);
        var pUpdateTime = cmd.Parameters.Add("@updateTime", SqliteType.Text);
        var pHashKey = cmd.Parameters.Add("@hashKey", SqliteType.Text);

        cmd.Prepare(); 

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
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM FastData WHERE Id = @id AND HashKey = @hashKey";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@hashKey", hashKey);

        return cmd.ExecuteNonQuery() > 0;
    }

    public int DeleteBulk(string hashKey, IEnumerable<string> ids)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = "DELETE FROM FastData WHERE Id = @id AND HashKey = @hashKey";

        var pId = cmd.Parameters.Add("@id", SqliteType.Text);
        var pHashKey = cmd.Parameters.Add("@hashKey", SqliteType.Text);

        cmd.Prepare(); 

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
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM FastData WHERE HashKey = @hashKey";
        cmd.Parameters.AddWithValue("@hashKey", hashKey);
        
        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 流式 JSON 搜索
    /// 注: C# 的 yield return 状态机会保证在枚举结束或被 break 时，安全释放 connection
    /// </summary>
    public IEnumerable<FastData> Search(string hashKey, JsonElement jsonQuery)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Content, HashKey, CreateTime, UpdateTime FROM FastData WHERE HashKey = @hashKey";
        cmd.Parameters.AddWithValue("@hashKey", hashKey);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var content = reader.GetString(1);
            var contentSpan = content.AsSpan().TrimStart();

            if (contentSpan.Length == 0 || (contentSpan[0] != '{' && contentSpan[0] != '['))
            {
                continue;
            }

            bool isMatch = false;
            try
            {
                // 将可能抛出异常的解析部分包裹在 try 中
                using var doc = JsonDocument.Parse(content);
                isMatch = JsonContains(doc.RootElement, jsonQuery);
            }
            catch (JsonException)
            {
                // Content 不是有效 JSON，跳过
            }

            // 将 yield return 移出 try-catch 块，完美解决 CS1626 报错
            if (isMatch)
            {
                yield return MapRow(reader);
            }
        }
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
    /// 计算 MD5 哈希 (支持超大文本内存池优化)
    /// </summary>
    public static string ComputeMd5(string input)
    {
        int maxByteCount = Encoding.UTF8.GetMaxByteCount(input.Length);

        if (maxByteCount <= 1024)
        {
            Span<byte> utf8Bytes = stackalloc byte[maxByteCount];
            int bytesWritten = Encoding.UTF8.GetBytes(input, utf8Bytes);

            Span<byte> hashBytes = stackalloc byte[16]; 
            MD5.HashData(utf8Bytes[..bytesWritten], hashBytes);
            return Convert.ToHexStringLower(hashBytes);
        }
        else
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(maxByteCount);
            try
            {
                int bytesWritten = Encoding.UTF8.GetBytes(input, buffer);
                Span<byte> hashBytes = stackalloc byte[16];
                MD5.HashData(buffer.AsSpan(0, bytesWritten), hashBytes);
                return Convert.ToHexStringLower(hashBytes);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}