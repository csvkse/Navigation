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

// 注册 FastDbService 为单例
builder.Services.AddSingleton<FastDbService>(_ => new FastDbService("fastdb.db"));

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
fastdb.MapGet("/doc", () => "FastDB is running!")
    .WithName("FastDBDoc")
    .Produces<string>();

// 获取当前 Key 下的所有数据
fastdb.MapGet("/", (string key, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    var list = db.GetByHashKey(hashKey);
    return list.Select(ToResult).ToArray();
}).WithName("FastDBGetAll")
  .Produces<FastDataResult[]>();

// 只读查询 - 通过 ID 的 GUID
fastdb.MapGet("/readonly", (Guid readOnlyId, FastDbService db) =>
{
    var data = db.GetByIdOnly(readOnlyId.ToString());
    if (data == null)
        return Results.NotFound();

    var list = db.GetByHashKey(data.HashKey);
    return Results.Ok(list.Select(ToResult).ToArray());
}).WithName("FastDBReadOnly")
  .Produces<FastDataResult[]>()
  .ProducesProblem(404);

// 根据 ID 获取指定数据
fastdb.MapGet("/{id}", (Guid id, string key, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    var data = db.GetById(id.ToString(), hashKey);
    return data != null ? Results.Ok(ToResult(data)) : Results.NotFound();
}).WithName("FastDBGetById")
  .Produces<FastDataResult>()
  .ProducesProblem(404);

// JSON 内部字段搜索
fastdb.MapPost("/search", (string key, JsonElement jsonQuery, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    var list = db.Search(hashKey, jsonQuery);
    return list.Select(ToResult).ToArray();
}).WithName("FastDBSearch")
  .Produces<FastDataResult[]>();

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
}).WithName("FastDBPost")
  .Produces<FastDataResult>();

// 更新数据
fastdb.MapPut("/{id}", (Guid id, string key, JsonElement data, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    var existing = db.GetById(id.ToString(), hashKey);
    if (existing == null)
        return Results.NotFound();

    db.Update(id.ToString(), hashKey, data.GetRawText(), DateTime.Now.ToString("o"));
    return Results.Ok(true);
}).WithName("FastDBPut")
  .Produces<bool>()
  .ProducesProblem(404);

// 删除数据
fastdb.MapDelete("/{id}", (Guid id, string key, FastDbService db) =>
{
    var hashKey = FastDbService.ComputeMd5(key);
    return db.Delete(id.ToString(), hashKey);
}).WithName("FastDBDelete")
  .Produces<bool>();

app.Run();

// ===== 辅助方法 =====

static FastDataResult ToResult(FastData item)
{
    object? content;
    try
    {
        content = JsonSerializer.Deserialize<JsonElement>(item.Content);
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
