using Lucidly.API.Infra;
using Lucidly.Common;
using Lucidly.Common.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using OllamaSharp.Models.Chat;
using OpenAI.Assistants;
using StackExchange.Redis;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Lucidly.API.Hubs;

public class SoloChatHub(Kernel kernel, McpClientManager mcpClientManager, GroupAccessor groupAccessor, FunctionApprovalStore functionApprovalStore) : Hub
{
    //, GroupStreamManager manager, RedisGroupListener redis
    private const string JokerName = "Joker";
    private const string JokerInstructions = "You are helpfull assistant.Have access of multiple tools." +
        "Use tools to respond. " +
        "Example: If input has `@xxx messgae`, xxx is the tool name, which you must invoke with message without toolname ";

    //public async Task JoinGroupForStreaming(string group)
    //{
    //    await redis.SubscribeGroup(group);
    //}
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        groupAccessor.Join(Context.ConnectionId, groupName);
    }
    //public  ChannelReader<ChatBubble> StreamGroup(string group, string message, List<McpClientConfigModel> availableTools, CancellationToken cancellationToken)
    //{
    //    var channelReader = manager.Subscribe(group);
    //    _ = WriteItemsToGroupAsync(group, message, availableTools, cancellationToken);

    //    return channelReader;

    //}
    //private async Task WriteItemsToGroupAsync( string group, string message, List<McpClientConfigModel> availableTools,
    //CancellationToken cancellationToken)
    //{
    //    Exception localException = null;
    //    await redis.PublishGroup(group, new ChatBubble(Guid.NewGuid().ToString(), "You", message));

    //    var alltools = await mcpClientManager.GetAllToolsAsync(availableTools);//[.. availableTools.Take(1)]
    //    try
    //    {
    //        var bubbleId = Guid.NewGuid().ToString();
    //        var executionSettings = new OllamaPromptExecutionSettings()
    //        {
    //            Temperature = 0,
    //            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true, }),
    //            ExtensionData = new Dictionary<string, object>
    //            {
    //                {"num_ctx","8192" },
    //                {"GroupName",group },
    //                {"BubbleId",bubbleId }
    //            },
    //        };
    //        var totalCompletion = new StringBuilder();
    //        ChatCompletionAgent agent =
    //        new()
    //        {
    //            Name = JokerName,
    //            Instructions = JokerInstructions,
    //            Kernel = kernel,
    //            Arguments = new KernelArguments(executionSettings) { ["GroupName"] = group }
    //        };

    //        agent.Kernel.Plugins.AddFromFunctions("Tools", alltools.Select(aiFunction => aiFunction.AsKernelFunction()));
    //        var content = new List<ChatMessageContent>
    //        {
    //            new(AuthorRole.User, message)
    //        };

          

    //        await foreach (AgentResponseItem<StreamingChatMessageContent> response in 
    //            agent.InvokeStreamingAsync(content , cancellationToken: cancellationToken))
    //        {
    //            totalCompletion.Append(response.Message.Content);
    //            await redis.PublishGroup(group, new ChatBubble(bubbleId, "AI", totalCompletion.ToString()));

    //        }
    //        await Clients.All.SendAsync("OnReceiveMessageEnd");
    //    }
    //    catch (Exception ex)
    //    {
    //        localException = ex;
    //    }
    //}

    
    public ChannelReader<ChatBubble> StreamMessage(string user, string message, List<McpClientConfigModel> availableTools,CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<ChatBubble>();
        _ = Task.Run(() => WriteItemsAsync(channel.Writer, user, message, availableTools, cancellationToken), cancellationToken);

        //_ = WriteItemsAsync(channel.Writer, user, message, availableTools, cancellationToken);

        return channel.Reader;
    }

    private async Task WriteItemsAsync(
        ChannelWriter<ChatBubble> writer,
      string user, string message, List<McpClientConfigModel> availableTools,
        CancellationToken cancellationToken)
    {
        var _kernel = kernel.Clone(); 

        Exception localException = null;
        var alltools = await mcpClientManager.GetAllToolsAsync(availableTools);
        try
        {
            await writer.WriteAsync(new ChatBubble(Guid.NewGuid().ToString(), user, message), cancellationToken);
             
            var executionSettings = new OllamaPromptExecutionSettings()
            {
                Temperature = 0,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true }),
                ExtensionData = new Dictionary<string, object>
                {
                    {"num_ctx","8192" }
                },
            };
            var totalCompletion = new StringBuilder();
            ChatCompletionAgent agent =
            new()
            {
                Name = JokerName,
                Instructions = JokerInstructions,
                Kernel = _kernel,
                Arguments = new KernelArguments(executionSettings),
                
            };
            var bubbleId = Guid.NewGuid().ToString();

            agent.Kernel.Plugins.AddFromFunctions("Tools", alltools.Select(aiFunction => aiFunction.AsKernelFunction()));
         
            agent.Kernel.FunctionInvocationFilters.Add(new FunctionCallsFilter(writer, new ChatBubble(bubbleId, agent.Name, ""), totalCompletion, functionApprovalStore));

            var content = new List<ChatMessageContent>
            {
                new(AuthorRole.User, message)
            }; 
            var agentStreamingResult = agent.InvokeStreamingAsync(content,/*options: agentInvokeOptions,*/ cancellationToken:cancellationToken);

            await foreach (var streamingUpdate in agentStreamingResult)
            {
                totalCompletion.Append(streamingUpdate.Message.Content);
                await writer.WriteAsync(new ChatBubble(bubbleId, agent.Name, totalCompletion.ToString()), cancellationToken);
            }

            await Clients.All.SendAsync("OnReceiveMessageEnd");
        }
        catch (Exception ex)
        {
            localException = ex;
        }
        finally
        {

            writer.Complete(localException);
        }
    }
    static async IAsyncEnumerable<int> RangeAsync(int start, int count)
{
  for (int i = 0; i < count; i++)
  {
    await Task.Delay(1000*i);
    yield return start + i;
  }
}
    public async Task OnCancel()
    {
        await Clients.All.SendAsync("OnReceiveMessageEnd");
    }
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", "You", Guid.NewGuid().ToString(), message);


        var mcpClient = await McpClientFactory.CreateAsync(new SseClientTransport(new SseClientTransportOptions
        {
            Endpoint = new("https://localhost:7046"),
            TransportMode = HttpTransportMode.AutoDetect,
            //AdditionalHeaders = model.Headers.ToDictionary(x => x.Key, x => x.Value)
        }));
        var availableTools = await mcpClient.ListToolsAsync();
        kernel.Plugins.AddFromFunctions("Tools", availableTools.Select(aiFunction => aiFunction.AsKernelFunction()));
        var executionSettings = new OllamaPromptExecutionSettings()
        {
            Temperature = 0,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
        };
        var totalCompletion = new StringBuilder();
        ChatCompletionAgent agent =
        new()
        {
            Name = JokerName,
            Instructions = JokerInstructions,
            Kernel = kernel,
            Arguments = new KernelArguments(executionSettings)
        };
        ChatMessageContent chatMessageContent = new(AuthorRole.User, message);

        var bubbleId = Guid.NewGuid().ToString();
        await foreach (AgentResponseItem<StreamingChatMessageContent> response in agent.InvokeStreamingAsync(chatMessageContent))
        {
            totalCompletion.Append(response.Message.Content);
            await Clients.All.SendAsync("ReceiveMessage", "AI", bubbleId, totalCompletion.ToString());

        }
        await Clients.All.SendAsync("OnReceiveMessageEnd");

    }

}
