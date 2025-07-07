using Lucidly.Common.Models;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Lucidly.API.Infra
{
    public class GroupStreamManager
    {
        private readonly ConcurrentDictionary<string, GroupChannel> _groups = new();

        public ChannelReader<ChatBubble> Subscribe(string groupName)
        {
            var channel = _groups.GetOrAdd(groupName, name => new GroupChannel(name));
            return channel.Subscribe();
        }

        public async Task BroadcastToGroupAsync(string groupName, ChatBubble message)
        {
            if (_groups.TryGetValue(groupName, out var group))
            {
                await group.WriteAsync(message);
            }
        }
    }
}
