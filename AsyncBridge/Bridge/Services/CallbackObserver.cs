using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using System.Text;

namespace Bridge.Services
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
                while (!stoppingToken.IsCancellationRequested)
                {
                    var subscription = await _queue.DequeueSubscriptionAsync(stoppingToken);

                    using (var channel = connection.CreateModel())
                    {
                        var consumer = new EventingBasicConsumer(channel);
                        consumer.Received += (model, ea) =>
                        {
                            var body = ea.Body;
                            var jobResult = Encoding.UTF8.GetString(body);
                            var jobId = ea.BasicProperties.CorrelationId;

                            WriteToRedis(jobId, jobResult);
                        };

                        channel.BasicConsume(queue: subscription,
                                             autoAck: true,
                                             consumer: consumer);
                    }
                }
            }
        }

        private static void WriteToRedis(string key, string value)
        {
            using (ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost"))
            {
                IDatabase db = redis.GetDatabase();

                db.StringSet(key, value);
            }
        }
    }
}
