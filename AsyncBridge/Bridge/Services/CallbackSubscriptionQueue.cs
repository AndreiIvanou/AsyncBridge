using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Bridge.Services
{
    public interface ICallbackSubscriptionQueue
    {
        void EnqueueSubscription(string subscription);

        Task<string> DequeueSubscriptionAsync(CancellationToken cancellationToken);
    }

    public class CallbackSubscriptionQueue : ICallbackSubscriptionQueue
    {
        private ConcurrentQueue<string> _workItems = new ConcurrentQueue<string>();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);

        public void EnqueueSubscription(string subscription)
        {
            _workItems.Enqueue(subscription);
            _signal.Release();
        }

        public async Task<string> DequeueSubscriptionAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var subscription);

            return subscription;
        }
    }
}
