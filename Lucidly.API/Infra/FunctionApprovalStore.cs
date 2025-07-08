using Lucidly.Common;
using System.Collections.Concurrent;

namespace Lucidly.API.Infra
{
    public class FunctionApprovalStore
    {
        private readonly ConcurrentDictionary<string, PendingFunctionCall> _approvals = new();

        public IEnumerable<PendingFunctionCall> GetPending() => _approvals.Values;

        public bool TryGet(string id, out PendingFunctionCall? call) => _approvals.TryGetValue(id, out call);

        public string Add(PendingFunctionCall call)
        {
            _approvals[call.Id] = call;
            return call.Id;
        }

        public bool Approve(string id) => TryComplete(id, true);
        public bool Reject(string id) => TryComplete(id, false);

        private bool TryComplete(string id, bool approved)
        {
            if (_approvals.TryRemove(id, out var call))
            {
                call.Tcs.TrySetResult(approved);
                return true;
            }
            return false;
        }
    }
}
