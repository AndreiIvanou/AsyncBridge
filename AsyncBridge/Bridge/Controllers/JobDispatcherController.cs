using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bridge.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace Bridge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobDispatcherController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get(string key)
        {
            using (ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost"))
            {
                IDatabase db = redis.GetDatabase();

                string result = db.StringGet(key);

                return Ok(result);
            }
        }

        [HttpPost]
        public void Post([FromBody] SubmitJobRequest request)
        {
            using (ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost"))
            {
                IDatabase db = redis.GetDatabase();

                db.StringSet(request.Name, request.JobParameters);
            }
        }

    }
}
