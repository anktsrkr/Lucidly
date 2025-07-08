using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lucidly.Common
{
    public class PendingFunctionCall
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Plugin { get; init; } = string.Empty;
        public string Function { get; init; } = string.Empty;
        public Dictionary<string, object?> Args { get; init; } = [];
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        [JsonIgnore]
        public TaskCompletionSource<bool> Tcs { get; init; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
