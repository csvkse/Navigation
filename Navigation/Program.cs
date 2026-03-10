using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Navigation;
using Scalar.AspNetCore;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 确保 data 目录存在，将数据库放在独立目录中以支持 Docker Volume 挂载
var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDir);
builder.Services.AddSingleton<FastDbService>(_ => new FastDbService(Path.Combine(dataDir, "fastdb.db")));

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
    var list = db.GetByHashKey(hashKey);
    return list.Select(ToResult).ToArray();
});

// 只读查询 - 通过 ID 的 GUID
fastdb.MapGet("/readonly", (Guid readOnlyId, FastDbService db) =>
{
    var data = db.GetByIdOnly(readOnlyId.ToString());
    if (data == null)
        return Results.NotFound();

    var list = db.GetByHashKey(data.HashKey);
    return Results.Ok(list.Select(ToResult).ToArray());
});

// 根据 ID 获取指定数据
fastdb.MapGet("/{id}", (Guid id, string key, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    var data = db.GetById(id.ToString(), hashKey);
    return data != null ? Results.Ok(ToResult(data)) : Results.NotFound();
});

// JSON 内部字段搜索
fastdb.MapPost("/search", (string key, JsonElement jsonQuery, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    var list = db.Search(hashKey, jsonQuery);
    return list.Select(ToResult).ToArray();
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

// 删除数据
fastdb.MapDelete("/{id}", (Guid id, string key, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    return db.Delete(id.ToString(), hashKey);
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

// ===== AOT JSON 序列化上下文 =====

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(FastData))]
[JsonSerializable(typeof(FastData[]))]
[JsonSerializable(typeof(FastDataResult))]
[JsonSerializable(typeof(FastDataResult[]))]
[JsonSerializable(typeof(JsonElement))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}