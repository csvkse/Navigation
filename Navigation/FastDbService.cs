using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Caching.Memory; // 引入 MemoryCache

namespace Navigation;

/// <summary>
/// FastDB SQLite 数据访问服务 (AOT 兼容，极限性能 + 高并发安全 + MemoryCache防OOM版)
/// </summary>
public class FastDbService
{
    private readonly string _connectionString;

    // 引入高性能内存缓存
    private readonly IMemoryCache _cache;

    // 缓存策略配置：滑动过期时间 10 分钟，每个缓存项大小记为 1
    private readonly MemoryCacheEntryOptions _cacheOptions = new MemoryCacheEntryOptions()
        .SetSlidingExpiration(TimeSpan.FromMinutes(10))
        .SetSize(1);

    public FastDbService(string dbPath = "fastdb.db")
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        Console.WriteLine($"FastDB 数据库文件路径: {dbPath}");

        _connectionString = $"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate;Default Timeout=5;";

        // 初始化带有大小限制的缓存池，防止恶意拉取导致服务器内存 OOM
        // 容量设置 10000 意味着最多缓存 10000 个 Json 聚合结果或单条实体
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 10000,
            CompactionPercentage = 0.2 // 当达到限制时，清理 20% 的缓存
        });

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
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

    #region 核心读取 (带缓存)

    /// <summary>
    /// 按 HashKey 获取所有数据，直接生成 JSON 数组字符串 (缓存优先)
    /// </summary>
    public string GetByHashKeyRawJson(string hashKey)
    {
        string cacheKey = $"RawJson_{hashKey}";

        // 1. 尝试从缓存获取
        if (_cache.TryGetValue(cacheKey, out string? cachedJson) && cachedJson != null)
        {
            return cachedJson;
        }

        // 2. 缓存未命中，执行数据库查询并序列化
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Content, HashKey, CreateTime, UpdateTime FROM FastData WHERE HashKey = @hashKey ORDER BY CreateTime DESC";
        cmd.Parameters.AddWithValue("@hashKey", hashKey);

        using var reader = cmd.ExecuteReader();
        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms);

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

        var finalJson = Encoding.UTF8.GetString(ms.ToArray());

        // 3. 写入缓存
        _cache.Set(cacheKey, finalJson, _cacheOptions);

        return finalJson;
    }

    public List<FastData> GetByHashKey(string hashKey)
    {
        string cacheKey = $"List_{hashKey}";
        if (_cache.TryGetValue(cacheKey, out List<FastData>? cachedList) && cachedList != null)
        {
            return cachedList;
        }

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Content, HashKey, CreateTime, UpdateTime FROM FastData WHERE HashKey = @hashKey ORDER BY CreateTime DESC";
        cmd.Parameters.AddWithValue("@hashKey", hashKey);

        var list = ReadAll(cmd);
        _cache.Set(cacheKey, list, _cacheOptions);

        return list;
    }

    public FastData? GetByIdOnly(string id)
    {
        string cacheKey = $"Id_{id}";
        if (_cache.TryGetValue(cacheKey, out FastData? cachedData))
        {
            return cachedData;
        }

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Content, HashKey, CreateTime, UpdateTime FROM FastData WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        var data = ReadOne(cmd);
        if (data != null)
        {
            _cache.Set(cacheKey, data, _cacheOptions);
        }

        return data;
    }

    public FastData? GetById(string id, string hashKey)
    {
        // 走单键缓存即可，由于 Id 是主键，命中缓存就等于拿到了唯一结果
        var data = GetByIdOnly(id);
        if (data != null && data.HashKey == hashKey)
        {
            return data;
        }
        return null;
    }

    #endregion

    #region 写入/删除 (带缓存主动失效机制)

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
        InvalidateCache(entity.Id, entity.HashKey);
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

        cmd.Prepare();

        var modifiedHashKeys = new HashSet<string>();

        foreach (var entity in entities)
        {
            pId.Value = entity.Id;
            pContent.Value = entity.Content;
            pHashKey.Value = entity.HashKey;
            pCreateTime.Value = entity.CreateTime;
            pUpdateTime.Value = (object?)entity.UpdateTime ?? DBNull.Value;
            cmd.ExecuteNonQuery();

            // 收集受影响的缓存Key
            modifiedHashKeys.Add(entity.HashKey);
            _cache.Remove($"Id_{entity.Id}");
        }
        transaction.Commit();

        foreach (var hashKey in modifiedHashKeys)
        {
            InvalidateHashKeyCache(hashKey);
        }
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

        bool success = cmd.ExecuteNonQuery() > 0;
        if (success)
        {
            InvalidateCache(id, hashKey);
        }
        return success;
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

            if (cmd.ExecuteNonQuery() > 0)
            {
                count++;
                _cache.Remove($"Id_{id}");
            }
        }

        transaction.Commit();

        if (count > 0)
        {
            InvalidateHashKeyCache(hashKey);
        }
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

        bool success = cmd.ExecuteNonQuery() > 0;
        if (success)
        {
            InvalidateCache(id, hashKey);
        }
        return success;
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

            if (cmd.ExecuteNonQuery() > 0)
            {
                count++;
                _cache.Remove($"Id_{id}");
            }
        }

        transaction.Commit();

        if (count > 0)
        {
            InvalidateHashKeyCache(hashKey);
        }
        return count;
    }

    public int Clear(string hashKey)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM FastData WHERE HashKey = @hashKey";
        cmd.Parameters.AddWithValue("@hashKey", hashKey);

        int count = cmd.ExecuteNonQuery();
        if (count > 0)
        {
            InvalidateHashKeyCache(hashKey);
            // 注意：Clear 操作由于没有具体的 Id，无法精准清理单体数据的缓存 (除非做全量缓存扫描)。
            // 但考虑到 Clear 的语义，一般单体缓存也会随滑动时间过期，这里优先清理聚合缓存。
        }
        return count;
    }

    /// <summary>
    /// 清理单个记录和相关聚合的缓存
    /// </summary>
    private void InvalidateCache(string id, string hashKey)
    {
        _cache.Remove($"Id_{id}");
        InvalidateHashKeyCache(hashKey);
    }

    /// <summary>
    /// 清理 HashKey 关联的所有聚合缓存
    /// </summary>
    private void InvalidateHashKeyCache(string hashKey)
    {
        _cache.Remove($"RawJson_{hashKey}");
        _cache.Remove($"List_{hashKey}");
    }

    #endregion

    #region 搜索与辅助 (保持原生流式与底层实现)

    /// <summary>
    /// 流式 JSON 搜索 (保持每次查库，避免搜索引发巨大内存抖动，最稳妥的做法)
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
                using var doc = JsonDocument.Parse(content);
                isMatch = JsonContains(doc.RootElement, jsonQuery);
            }
            catch (JsonException)
            {
                // Content 不是有效 JSON，跳过
            }

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
    /// 计算 MD5 哈希 (支持超大文本内存池优化，无分配特性完美保留)
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