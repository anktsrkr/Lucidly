using Lucidly.Common.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace Lucidly.API.Infra
{
    public class RedisGroupListener
    {
        private readonly ISubscriber _subscriber;
        private readonly GroupStreamManager _streamManager;
        public RedisGroupListener(IConnectionMultiplexer redis, GroupStreamManager manager)
        {
            _subscriber = redis.GetSubscriber();
            _streamManager = manager;
        }

        public Task SubscribeGroup(string groupName)
        {
            return _subscriber.SubscribeAsync($"group:{groupName}", async (channel, value) =>
            {
               
                var message = JsonSerializer.Deserialize<ChatBubble>(value, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                await _streamManager.BroadcastToGroupAsync(groupName, message);
            });
        }

        public Task PublishGroup(string groupName, ChatBubble message)
        {
        
            return _subscriber.PublishAsync($"group:{groupName}", JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }
}
