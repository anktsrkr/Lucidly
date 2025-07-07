using System.Collections.Concurrent;

namespace Lucidly.UI.Utils
{
    public class OAuthFlowService
    {
        private static ConcurrentDictionary<string, TaskCompletionSource<string?>> _pending =
            new ConcurrentDictionary<string, TaskCompletionSource<string?>>();

        public string StartFlow()
        {
            var token = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<string?>();
            _pending[token] = tcs;
            return token;
        }

        public Task<string?> WaitForCodeAsync(string token)
        {
            return _pending[token].Task;
        }

        public void SetCode(string token, string? code)
        {
            if (_pending.TryRemove(token, out var tcs))
            {
                tcs.TrySetResult(code);
            }
        }
    }
}
