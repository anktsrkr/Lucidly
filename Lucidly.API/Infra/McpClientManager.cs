using Lucidly.Common;
using Lucidly.Common.Models;
using Microsoft.Extensions.Caching.Memory;
using ModelContextProtocol.Client;

namespace Lucidly.API.Infra
{
    public class McpClientManager(IMemoryCache memoryCache)
    {
        private readonly MemoryCacheEntryOptions _cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(30) // Auto-evict if not used for 30 minutes
        };

        // Create or get a cached client for a given server
        public async Task<IMcpClient> GetOrCreateClientAsync(string serverUrl, McpClientConfigModel config)
        {
            if (memoryCache.TryGetValue(serverUrl, out IMcpClient? client))
            {
                return client!;
            }
            var sseClientTransportOptions = new SseClientTransport(new SseClientTransportOptions
            {
                Endpoint = new(config.Url),
                AdditionalHeaders = config.McpClientAuthenticationMode == McpClientAuthenticationMode.ApiKey ? new Dictionary<string, string> { { config.ApiKeyName, config.ApiKeyValue } } : new Dictionary<string, string> { { "Authorization", $"Bearer {config.AccessToken!}" } },
                TransportMode = config.MCPTransportMode == MCPTransportMode.Sse ? HttpTransportMode.Sse : HttpTransportMode.StreamableHttp,
            });

            client =  await McpClientFactory.CreateAsync(sseClientTransportOptions);
            memoryCache.Set(serverUrl, client, _cacheOptions);

            return client;

        }

        // Get or fetch tools for a server
        public async Task<IList<McpClientTool>> GetOrFetchToolsAsync(string serverUrl, McpClientConfigModel config)
        {
            string toolsKey = $"tools::{serverUrl}";
            if (memoryCache.TryGetValue(toolsKey, out IList<McpClientTool>? tools))
            {
                return tools!;
            }

            var client = await GetOrCreateClientAsync(serverUrl, config);
            tools = await client.ListToolsAsync();
            memoryCache.Set(toolsKey, tools, _cacheOptions);

            return tools;
        }

        /// <summary>
        /// Get all tools from all provided servers.
        /// </summary>
        /// <param name="serverConfigs">Dictionary of serverUrl => McpClientConfig</param>
        /// <returns>List of all tools across all servers. Each tool's ServerUrl property will be set.</returns>
        public async Task<IList<McpClientTool>> GetAllToolsAsync(List<McpClientConfigModel> serverConfigs)
        {
            var allTools = new List<McpClientTool>();
            var tasks = serverConfigs.Select(async kvp => {
                var tools = await GetOrFetchToolsAsync(kvp.Name, kvp);
                return tools;
            });

            var results = await Task.WhenAll(tasks);
            foreach (var toolList in results)
            {
                allTools.AddRange(toolList);
            }
            return allTools;
        }
    }
}
