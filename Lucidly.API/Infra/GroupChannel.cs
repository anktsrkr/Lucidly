using Lucidly.Common.Models;
using System.Threading.Channels;

namespace Lucidly.API.Infra
{
    public class GroupChannel
    {
        private readonly Channel<ChatBubble> _channel;
        private int _subscribers = 0;
        private readonly object _lock = new();
        private readonly Timer _idleTimer;
        public string GroupName { get; }

        public GroupChannel(string groupName)
        {
            GroupName = groupName;
            _channel = Channel.CreateUnbounded<ChatBubble>();
            _idleTimer = new Timer(CloseIfIdle, null, Timeout.Infinite, Timeout.Infinite);
        }
        public ChannelReader<ChatBubble> Subscribe()
        {
            lock (_lock)
            {
                _subscribers++;
                _idleTimer.Change(Timeout.Infinite, Timeout.Infinite); // Cancel idle timeout
            }

            return WrapReader(_channel.Reader);
        }
        public void Unsubscribe()
        {
            lock (_lock)
            {
                _subscribers--;
                if (_subscribers <= 0)
                {
                    _idleTimer.Change(TimeSpan.FromSeconds(15), Timeout.InfiniteTimeSpan);
                }
            }
        }
        public async Task WriteAsync(ChatBubble message)
        {
            Console.WriteLine($"[Broadcast] Pushing to group  {message.Message}");

            await _channel.Writer.WriteAsync(message);
        }

        private ChannelReader<ChatBubble> WrapReader(ChannelReader<ChatBubble> inner)
        {
            var wrapper = Channel.CreateUnbounded<ChatBubble>();

            var readerTask = Task.Run(async () =>
            {
                try
                {
                    await foreach (var msg in inner.ReadAllAsync())
                    {
                        await wrapper.Writer.WriteAsync(msg);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Stream error] {ex.Message}");
                }
                finally
                {
                    wrapper.Writer.TryComplete();
                }
            });

            // Return the reader — DO NOT consume it again here
            return wrapper.Reader;
        }

        private void CloseIfIdle(object? state)
        {
            lock (_lock)
            {
                if (_subscribers <= 0)
                {
                    _channel.Writer.TryComplete();
                    _idleTimer.Dispose();
                }
            }
        }
    }
}
