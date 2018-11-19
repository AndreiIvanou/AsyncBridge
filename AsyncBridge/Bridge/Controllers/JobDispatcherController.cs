using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bridge.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace Bridge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobDispatcherController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get(string jobId)
        {
            using (ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost"))
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
            string jobId = SendToRabbitMq(request.Name, request.JobParameters);

            return Accepted(jobId);
        }

        private static string SendToRabbitMq(string jobName, string jsonRequest)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.CorrelationId = Guid.NewGuid().ToString();

                channel.ExchangeDeclare(exchange: "jobs", type: "topic");

                var routingKey = jobName;
                var message = jsonRequest;
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: "jobs",
                                     routingKey: routingKey,
                                     basicProperties: properties,
                                     body: body);

                WriteToRedis(properties.CorrelationId, "In progress");

                return properties.CorrelationId;
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
