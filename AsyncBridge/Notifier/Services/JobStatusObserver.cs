using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _config;

        public JobStatusObserver(IConfiguration config, IHubContext<NotificationHub> hubContext)
        {
            _config = config;
            _hubContext = hubContext;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            string redisAddress = _config["REDIS_ADDRESS"];

            _redis = ConnectionMultiplexer.Connect(redisAddress);

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
