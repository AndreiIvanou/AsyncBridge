using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Threading;

namespace Worker
{
    class Program
    {
        class Parameters
        {
            public int Number { get; set; }
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Started on Thread: {0}", Thread.CurrentThread.ManagedThreadId);
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "jobs", type: "topic");
                var queueName = channel.QueueDeclare(durable: true).QueueName;


                channel.QueueBind(queue: queueName, 
                                  exchange: "jobs",
                                  routingKey: "fibonacci");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    var routingKey = ea.RoutingKey;
                    var jobId = ea.BasicProperties.CorrelationId;

                    Console.WriteLine("Received message: {0}", message);
                    Console.WriteLine("Processing on Thread: {0}", Thread.CurrentThread.ManagedThreadId);
                    Console.WriteLine("Correlation Id: {0}", jobId);

                    Parameters parameters = JsonConvert.DeserializeObject<Parameters>(message);

                    int result = Fib(parameters.Number);
                    Console.WriteLine("Fibonacci: {0}", result);

                    using (ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost"))
                    {
                        IDatabase db = redis.GetDatabase();

                        db.StringSet(jobId, result);
                    }
                };
                channel.BasicConsume(queue: queueName,
                                     autoAck: true,
                                     consumer: consumer);

                Console.WriteLine("Waiting on Thread: {0}", Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine("Press [enter] to exit.");
                Console.ReadLine();
            }
        }

        private static int Fib(int n)
        {
            if (n == 0 || n == 1)
            {
                return n;
            }

            return Fib(n - 1) + Fib(n - 2);
        }
    }
}