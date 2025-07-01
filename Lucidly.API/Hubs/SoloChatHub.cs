using Lucidly.Common.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using ModelContextProtocol.Client;
using System.Text;
using System.Threading.Channels;

namespace Lucidly.API.Hubs;

public class SoloChatHub(Kernel kernel) : Hub
{
    private const string JokerName = "Joker";
    private const string JokerInstructions = "You are helpfull assistant.Have access of multiple tools." +
        "Use tools to respond. " +
        "Example: If input has @xxx, xxx is the tool name, which you must invoke ";

    public ChannelReader<ChatBubble> StreamMessage(string user, string message, CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<ChatBubble>();

        _ = WriteItemsAsync(channel.Writer, user, message, cancellationToken);

        return channel.Reader;
    }

    private async Task WriteItemsAsync(
        ChannelWriter<ChatBubble> writer,
      string user, string message,
        CancellationToken cancellationToken)
    {
        Exception localException = null;
        try
        {
            await writer.WriteAsync(new ChatBubble(Guid.NewGuid().ToString(), user, message), cancellationToken);

            var mcpClient = await McpClientFactory.CreateAsync(new SseClientTransport(new SseClientTransportOptions
            {
                Endpoint = new("https://remote.mcpservers.org/sequentialthinking/mcp"),
                TransportMode = HttpTransportMode.AutoDetect,
                //AdditionalHeaders = model.Headers.ToDictionary(x => x.Key, x => x.Value)
            }));
            var availableTools = await mcpClient.ListToolsAsync();
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
                Kernel = kernel,
                Arguments = new KernelArguments(executionSettings),
                
            };

           agent.Kernel.Plugins.AddFromFunctions("Tools", availableTools.Select(aiFunction => aiFunction.AsKernelFunction()));

            var content = new List<ChatMessageContent>
            {
                new(new AuthorRole("control"), "thinking"),
                new(AuthorRole.User, message)
            };

            var bubbleId = Guid.NewGuid().ToString();

            await foreach (AgentResponseItem<StreamingChatMessageContent> response in agent.InvokeStreamingAsync(content, cancellationToken: cancellationToken))
            {
                totalCompletion.Append(response.Message.Content);
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
