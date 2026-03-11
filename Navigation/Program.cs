using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Navigation;
using Scalar.AspNetCore;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 将数据库放在独立目录中以支持 Docker Volume 挂载
var dbPath = Path.Combine(AppContext.BaseDirectory, "data", "fastdb.db");
builder.Services.AddSingleton<FastDbService>(_ => new FastDbService(dbPath));


// 添加 CORS 服务
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// 使用 CORS 中间件
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "/openapi/{documentName}.json";
    });
    app.MapScalarApiReference();
}

app.UseStaticFiles(); // 启用 wwwroot 目录下静态文件的访问
// 如果是单页应用 (SPA):
app.MapFallbackToFile("index.html");

// ===== FastDB 路由组 =====
var fastdb = app.MapGroup("/fastdb");

// 文档/健康检查
fastdb.MapGet("/doc", () => "FastDB is running!");

// 获取当前 Key 下的所有数据
fastdb.MapGet("/", (string key, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    var rawJson = db.GetByHashKeyRawJson(hashKey);
    return Results.Content(rawJson, "application/json");
});

// 获取指定 Key 的只读 GUID（如果没有则自动创建并返回）
fastdb.MapGet("/readonly/guid", (string key, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    var guid = db.GetOrCreateReadOnlyGuid(hashKey);
    return Results.Ok(new { guid = guid });
});

// 只读查询 - 通过 ID 的 GUID
fastdb.MapGet("/readonly", (Guid readOnlyId, FastDbService db) =>
{
    var data = db.GetByIdOnly(readOnlyId.ToString());
    if (data == null)
        return Results.NotFound();

    var rawJson = db.GetByHashKeyRawJson(data.HashKey);
    return Results.Content(rawJson, "application/json");
});

// 根据 ID 获取指定数据
fastdb.MapGet("/{id}", (Guid id, string key, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    var data = db.GetById(id.ToString(), hashKey);
    return data != null ? Results.Ok(ToResult(data)) : Results.NotFound();
});

// JSON 内部字段搜索
fastdb.MapPost("/search", ([FromQuery] string key, [FromBody] JsonElement jsonQuery, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    var list = db.Search(hashKey, jsonQuery);

    // 移除 .ToArray()，利用框架的流式序列化
    return list.Select(ToResult);
});

// 新增数据
fastdb.MapPost("/", (string key, JsonElement data, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    var entity = new FastData
    {
        Id = Guid.NewGuid().ToString(),
        Content = data.GetRawText(),
        HashKey = hashKey,
        CreateTime = DateTime.Now.ToString("o")
    };
    db.Insert(entity);
    return ToResult(entity);
});

// 批量新增数据
fastdb.MapPost("/bulk", (string key, JsonElement dataList, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    if (dataList.ValueKind != JsonValueKind.Array)
        return Results.BadRequest("Expected a JSON array.");

    var entities = new List<FastData>();
    var now = DateTime.Now.ToString("o");
    foreach (var data in dataList.EnumerateArray())
    {
        entities.Add(new FastData
        {
            Id = Guid.NewGuid().ToString(),
            Content = data.GetRawText(),
            HashKey = hashKey,
            CreateTime = now
        });
    }
    
    db.InsertBulk(entities);
    return Results.Ok(entities.Count.ToString());
});

// 更新数据
fastdb.MapPut("/{id}", (Guid id, string key, JsonElement data, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    var existing = db.GetById(id.ToString(), hashKey);
    if (existing == null)
        return Results.NotFound();

    db.Update(id.ToString(), hashKey, data.GetRawText(), DateTime.Now.ToString("o"));
    return Results.Ok(true);
});

// 批量更新数据
fastdb.MapPut("/bulk", (string key, JsonElement dataList, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    if (dataList.ValueKind != JsonValueKind.Array)
        return Results.BadRequest("Expected a JSON array.");

    var items = new List<(string Id, string Content)>();
    foreach (var item in dataList.EnumerateArray())
    {
        if (!item.TryGetProperty("id", out var idProp))
            continue;

        var itemId = idProp.GetString();
        if (string.IsNullOrEmpty(itemId))
            continue;

        // 从 item 中提取 content：移除 id 字段，其余作为 content
        if (item.TryGetProperty("content", out var contentProp))
        {
            items.Add((itemId, contentProp.GetRawText()));
        }
        else
        {
            // 如果没有 content 字段，则把整个对象（去掉 id）作为 content
            var dict = new Dictionary<string, JsonElement>();
            foreach (var prop in item.EnumerateObject())
            {
                if (prop.Name != "id")
                    dict[prop.Name] = prop.Value.Clone();
            }
            items.Add((itemId, JsonSerializer.Serialize(dict, AppJsonSerializerContext.Default.DictionaryStringJsonElement)));
        }
    }

    if (items.Count == 0)
        return Results.BadRequest("No valid items to update.");

    var count = db.UpdateBulk(hashKey, items);
    return Results.Ok(count);
});

// 删除数据
fastdb.MapDelete("/{id}", (Guid id, string key, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    return db.Delete(id.ToString(), hashKey);
});

// 批量删除数据
fastdb.MapPost("/bulk-delete", (string key, JsonElement dataList, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    if (dataList.ValueKind != JsonValueKind.Array)
        return Results.BadRequest("Expected a JSON array of IDs.");

    var ids = new List<string>();
    foreach (var item in dataList.EnumerateArray())
    {
        var id = item.GetString();
        if (!string.IsNullOrEmpty(id))
            ids.Add(id);
    }

    if (ids.Count == 0)
        return Results.BadRequest("No valid IDs to delete.");

    var count = db.DeleteBulk(hashKey, ids);
    return Results.Ok(count);
});

// 清空缓存/删除某个Key下所有数据
fastdb.MapDelete("/clear", (string key, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    var deletedCount = db.Clear(hashKey);
    return Results.Ok(deletedCount.ToString());
});

app.Run();

// ===== 辅助方法 =====

static FastDataResult ToResult(FastData item)
{
    object? content;
    try
    {
        content = JsonSerializer.Deserialize(item.Content, AppJsonSerializerContext.Default.JsonElement);
    }
    catch
    {
        content = item.Content;
    }

    return new FastDataResult
    {
        Id = item.Id,
        Content = content,
        HashKey = item.HashKey,
        CreateTime = item.CreateTime,
        UpdateTime = item.UpdateTime
    };
}