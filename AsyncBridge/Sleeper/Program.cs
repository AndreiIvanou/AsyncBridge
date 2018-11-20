﻿using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
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
            Console.WriteLine("Started on Thread: {0}", Thread.CurrentThread.ManagedThreadId);
            var factory = new ConnectionFactory() { HostName = "localhost" };
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

                    Console.WriteLine("Received message: {0}", message);
                    Console.WriteLine("Processing on Thread: {0}", Thread.CurrentThread.ManagedThreadId);
                    Console.WriteLine("Correlation Id: {0}", jobId);

                    Parameters parameters = JsonConvert.DeserializeObject<Parameters>(message);

                    Thread.Sleep(parameters.Number);
                    Console.WriteLine("Awoke on Thread: {0}", Thread.CurrentThread.ManagedThreadId);
                };

                channel.BasicConsume(queue: "sleeper.work.queue",
                                     autoAck: true,
                                     consumer: consumer);

                Console.WriteLine("Waiting on Thread: {0}", Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine("Press [enter] to exit.");
                Console.ReadLine();
            }
        }
    }
}