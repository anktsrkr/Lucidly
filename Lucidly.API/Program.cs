using Lucidly.API.Hubs;
using Lucidly.API.Infra;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.SemanticKernel;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
//    ConnectionMultiplexer.Connect("localhost")); // Use config or env var for production

// Add group stream manager
//builder.Services.AddSingleton<GroupStreamManager>();
builder.Services.AddSingleton<GroupAccessor>();
// Add Redis group listener
//builder.Services.AddSingleton<RedisGroupListener>();
builder.Services.AddSignalR();
builder.Services.AddKernel();
//builder.Services.AddSingleton<IFunctionInvocationFilter, FunctionCallsFilter>();
//builder.Services.AddSingleton<IAutoFunctionInvocationFilter, AutoFunctionCallsFilter>();
builder.Services.AddOpenAIChatCompletion("qwen/qwen3-8b",  new Uri("http://localhost:11435/v1"), apiKey:"");
//builder.Services.AddOllamaChatCompletion("hf.co/Qwen/Qwen3-4B-GGUF:Q8_0", new Uri("http://localhost:11434/"));//hf.co/ibm-granite/granite-3.3-8b-instruct-GGUF:Q5_K_M
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<McpClientManager>(); builder.Services.AddSingleton<FunctionApprovalStore>();

var app = builder.Build();

app.UseResponseCompression();

app.UseHttpsRedirection();

app.MapHub<SoloChatHub>("/solo");
var approvals = app.MapGroup("/api/approvals");

approvals.MapGet("/pending", (FunctionApprovalStore store) =>
{
    return Results.Ok(store.GetPending());
});

approvals.MapPost("/approve/{id}", (string id, FunctionApprovalStore store) =>
{
    return store.Approve(id) ? Results.Ok() : Results.NotFound();
});

approvals.MapPost("/reject/{id}", (string id, FunctionApprovalStore store) =>
{
    return store.Reject(id) ? Results.Ok() : Results.NotFound();
});
await app.RunAsync();

