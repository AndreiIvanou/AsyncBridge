using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bridge.Models;
using Bridge.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace Bridge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobDispatcherController : ControllerBase
    {
        private IConfiguration _config;
        private readonly IBackgroundTaskQueue _queue;

        public JobDispatcherController(IConfiguration config, IBackgroundTaskQueue queue)
        {
            _config = config;
            _queue = queue;
        }
        
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
            string jobId = Dispatch(request.JobName, request.JobParameters);

            _queue.QueueBackgroundWorkItem(async token =>
            {
                var id = Thread.CurrentThread.ManagedThreadId;
                await Task.Delay(TimeSpan.FromSeconds(5), token);
            });


            return Accepted(jobId);
        }

        private string Dispatch(string jobName, string jsonRequest)
        {
            var routingKey = _config[jobName + ":workqueue"];
            var callbackRoutingKey = _config[jobName + ":callbackqueue"];

            var factory = new ConnectionFactory() { HostName = "localhost" };
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
