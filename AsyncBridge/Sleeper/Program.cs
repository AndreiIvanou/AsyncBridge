using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;

namespace Sleeper
{
    class Program
    {
        class Parameters
        {
            public int Number { get; set; }
        }

        static void Main(string[] args)
        {
            string rabbitmqAddress = Environment.GetEnvironmentVariable("RABBITMQ_ADDRESS");

            Console.WriteLine("Started on Thread: {0}", Thread.CurrentThread.ManagedThreadId);
            var factory = new ConnectionFactory() { HostName = rabbitmqAddress };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    var routingKey = ea.RoutingKey;
                    var jobId = ea.BasicProperties.CorrelationId;
                    var replyTo = ea.BasicProperties.ReplyTo;

                    Console.WriteLine("Received message: {0}", message);
                    Console.WriteLine("Processing on Thread: {0}", Thread.CurrentThread.ManagedThreadId);
                    Console.WriteLine("Correlation Id: {0}", jobId);
                    Console.WriteLine("Callback Queue: {0}", replyTo);

                    Parameters parameters = JsonConvert.DeserializeObject<Parameters>(message);

                    Thread.Sleep(parameters.Number);
                    Console.WriteLine("Awoke on Thread: {0}", Thread.CurrentThread.ManagedThreadId);
                    SendResult(connection, channel, jobId, replyTo);
                };

                channel.BasicConsume(queue: "sleeper.work.queue",
                                     autoAck: true,
                                     consumer: consumer);

                Console.WriteLine("Waiting on Thread: {0}", Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine("Press [enter] to exit.");
                Console.ReadLine();
            }
        }

        private static void SendResult(IConnection connection, IModel channel, string jobId, string replyTo)
        {
            using (var callbackChannel = connection.CreateModel())
            {
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.CorrelationId = jobId;

                var resultMessage = $"Done at {DateTime.Now}.";
                var resultBody = Encoding.UTF8.GetBytes(resultMessage);

                callbackChannel.BasicPublish(exchange: "callback.exchange",
                             routingKey: replyTo,
                             basicProperties: properties,
                             body: resultBody);
            }
        }
    }
}
