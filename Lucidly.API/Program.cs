using Lucidly.API.Hubs;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Net.Http.Headers;
using Microsoft.SemanticKernel;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddKernel();
builder.Services.AddOllamaChatCompletion("hf.co/Qwen/Qwen3-4B-GGUF:Q8_0", new Uri("http://localhost:11434/"));//hf.co/ibm-granite/granite-3.3-8b-instruct-GGUF:Q5_K_M
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});

var app = builder.Build();

app.UseResponseCompression();

app.UseHttpsRedirection();

app.MapHub<SoloChatHub>("/solo");

await app.RunAsync();

