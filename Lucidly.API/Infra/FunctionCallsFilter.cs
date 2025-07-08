using Lucidly.Common;
using Lucidly.Common.Models;
using Microsoft.Extensions.Primitives;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;

namespace Lucidly.API.Infra
{
    public class FunctionCallsFilter(ChannelWriter<ChatBubble> writer, ChatBubble chatBubble, StringBuilder totalCompletion, FunctionApprovalStore store) : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            var args = context.Arguments?.Names.Zip(context.Arguments?.Values, (key, value) => new { key, value })
                     .ToDictionary(x => x.key, x => (object?)x.value) ?? new();

            var call = new PendingFunctionCall
            {
                Plugin = context.Function.PluginName!,
                Function = context.Function.Name!,
                Args = args,
            };

            store.Add(call);


            var toolUpdate = new ToolDetails
            {
                PluginName = call.Plugin,
                FunctionName = call.Function,
                FunctionArgs = call.Args,
                Type = "function-request"
            };
            totalCompletion.Append($"<tool_update>{JsonSerializer.Serialize(toolUpdate)}</tool_update>");

            chatBubble = chatBubble with
            {
                Message = totalCompletion.ToString(),
                Type = "function-request",
                PendingFunctionCall = call
            };

            await writer.WriteAsync(chatBubble);

            var approved = await call.Tcs.Task;
            toolUpdate.Type = approved ? "function-approved" : "function-rejected";
            totalCompletion.Append($"<tool_update>{JsonSerializer.Serialize(toolUpdate)}</tool_update>");

            var statusBubble = chatBubble with
            {
                Message = totalCompletion.ToString(),
                Type = approved ? "function-approved" : "function-rejected",
                PendingFunctionCall = call
            };
            await writer.WriteAsync(statusBubble);
            if (!approved)
            {
                context.Result = new FunctionResult(context.Result, "Function call rejected by user.");
                return;
            }

            await next(context);
            toolUpdate.Type = "function-result";

            toolUpdate.Result = context.Result.GetValue<dynamic>();

            totalCompletion.Append($"<tool_update>{JsonSerializer.Serialize(toolUpdate)}</tool_update>");

             statusBubble = chatBubble with
            {
                Message = totalCompletion.ToString(),
                Type = "function-result",
                PendingFunctionCall = call
            };
            //await writer.WriteAsync(statusBubble);

        }
    }
    public class AutoFunctionCallsFilter(ChannelWriter<ChatBubble> writer , ChatBubble chatBubble,StringBuilder totalCompletion, FunctionApprovalStore store) : IAutoFunctionInvocationFilter
    {
        private readonly FunctionApprovalStore _store = store;

        public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
        {
            var chatHistory = context.ChatHistory;
            var functionCalls = FunctionCallContent.GetFunctionCalls(chatHistory.Last()).ToArray();

            if (functionCalls is { Length: > 0 })
            {
                foreach (var functionCall in functionCalls)
                {
                     
                }
            }
            await next(context);

        }
    }
}
