﻿using System;
using System.Text;
using Dispatcher.Models;
using Dispatcher.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace Dispatcher.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobDispatcherController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ICallbackSubscriptionQueue _queue;

        public JobDispatcherController(IConfiguration config, ICallbackSubscriptionQueue queue)
        {
            _config = config;
            _queue = queue;
        }
        
        [HttpGet]
        public ActionResult<string> Get(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                return BadRequest();

            string redisAddress = _config["REDIS_ADDRESS"];

            using (ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(redisAddress))
            {
                IDatabase db = redis.GetDatabase();

                RedisValue jobResult = db.StringGet(jobId);

                if(jobResult == RedisValue.Null)
                {
                    return NotFound();
                }

                return Ok(jobResult);
            }
        }

        [HttpPost]
        public ActionResult<string> Post([FromBody] SubmitJobRequest request)
        {
            string jobId = Dispatch(request.JobName, request.JobParameters);

            return Accepted(jobId);
        }

        private string Dispatch(string jobName, string jsonRequest)
        {
            var routingKey = _config[jobName + ":workqueue"];
            var callbackRoutingKey = _config[jobName + ":callbackqueue"];
            string rabbitmqAddress = _config["RABBITMQ_ADDRESS"];

            var factory = new ConnectionFactory() { HostName = rabbitmqAddress };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.CorrelationId = Guid.NewGuid().ToString();
                properties.ReplyTo = callbackRoutingKey;
                               
                var message = jsonRequest;
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: "work.exchange",
                                     routingKey: routingKey,
                                     basicProperties: properties,
                                     body: body);

                WriteToRedis(properties.CorrelationId, $"Since {DateTime.Now} is in progress.");

                _queue.EnqueueSubscription(callbackRoutingKey);

                return properties.CorrelationId;
            }
        }

        private void WriteToRedis(string key, string value)
        {
            string redisAddress = _config["REDIS_ADDRESS"];
            using (ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(redisAddress))
            {
                IDatabase db = redis.GetDatabase();

                db.StringSet(key, value);
            }
        }
    }
}
