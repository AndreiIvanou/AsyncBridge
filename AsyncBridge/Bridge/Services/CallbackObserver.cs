using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using System.Text;

namespace Dispatcher.Services
{
    public class CallbackObserver : BackgroundService
    {
        private readonly ICallbackSubscriptionQueue _queue;

        public CallbackObserver(ICallbackSubscriptionQueue queue)
        {
            _queue = queue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            {
                List<IModel> channels = new List<IModel>();

                while (!stoppingToken.IsCancellationRequested)
                {
                    IModel channel = await SubscribeToCallbackQueue(connection, stoppingToken);
                    channels.Add(channel);
                }

                foreach(var channel in channels)
                {
                    channel.Close();
                    channel.Dispose();
                }
            }
        }

        private async Task<IModel> SubscribeToCallbackQueue(IConnection connection, CancellationToken stoppingToken)
        {
            var subscription = await _queue.DequeueSubscriptionAsync(stoppingToken);

            var channel = connection.CreateModel();

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var jobId = ea.BasicProperties.CorrelationId;
                var body = ea.Body;
                var jobResult = Encoding.UTF8.GetString(body);

                WriteToRedis(jobId, jobResult);
            };

            channel.BasicConsume(queue: subscription,
                                 autoAck: true,
                                 consumer: consumer);

            return channel;
        }

        private static void WriteToRedis(string jobId, string jobResult)
        {
            using (ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost"))
            {
                IDatabase db = redis.GetDatabase();

                db.StringSet(jobId, jobResult);

                var pub = redis.GetSubscriber();

                pub.Publish("jobnotifications", jobId);
            }
        }
    }
}
