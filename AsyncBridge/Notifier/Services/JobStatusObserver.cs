using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Notifier.Hubs;
using StackExchange.Redis;
using System.Threading;
using System.Threading.Tasks;

namespace Notifier.Services
{
    public class JobStatusObserver : IHostedService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private ConnectionMultiplexer _redis;

        public JobStatusObserver(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _redis = ConnectionMultiplexer.Connect("localhost");

            var sub = _redis.GetSubscriber();

            sub.Subscribe("jobnotifications", (channel, jobId) =>
            {
                IDatabase db = _redis.GetDatabase();

                string jobResult = db.StringGet((string)jobId);

                _hubContext.Clients.Groups(jobId).SendAsync("ReceiveJobResult", jobResult);
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _redis.Close();
            _redis.Dispose();

            return Task.CompletedTask;
        }
    }
}
