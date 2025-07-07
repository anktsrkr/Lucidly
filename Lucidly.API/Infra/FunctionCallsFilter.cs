using Lucidly.Common.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using StackExchange.Redis;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;

namespace Lucidly.API.Infra
{
    public class FunctionCallsFilter : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            var chatHistory = context.Arguments;

            await next(context);
        }
    }
    public class AutoFunctionCallsFilter(ChannelWriter<ChatBubble> writer , ChatBubble chatBubble) : IAutoFunctionInvocationFilter
    {
        public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
        {
            var chatHistory = context.ChatHistory;
            var functionCalls = FunctionCallContent.GetFunctionCalls(chatHistory.Last()).ToArray();

            if (functionCalls is { Length: > 0 })
            {
                foreach (var functionCall in functionCalls)
                {
                    var toolUpdate = new 
                    {
                        PluginName = functionCall.PluginName!,
                        FunctionName = functionCall.FunctionName!,
                        FunctionArgs = functionCall.Arguments?.Names.Zip(functionCall.Arguments?.Values, (key, value) => new { key, value })
                                                           .ToDictionary(x => x.key, x => x.value)
                    };
                    chatBubble = chatBubble with { Message = JsonSerializer.Serialize(toolUpdate) };
                    await writer.WriteAsync(chatBubble);
                }
            }

            await next(context);
           
        }
    }
}
