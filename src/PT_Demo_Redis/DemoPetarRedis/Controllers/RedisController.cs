using Microsoft.AspNetCore.Mvc;
using DemoPetarRedis.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace DemoPetarRedis.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RedisController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        var options = ConfigurationOptions.Parse("127.0.0.1:6379,allowAdmin=true");
        using (ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(options))
        {
            var server = redis.GetServer("127.0.0.1", 6379);
            var db = redis.GetDatabase();
            var keys = server.Keys(pattern: "*").ToArray(); // Use with caution; consider specifying a more specific pattern

            List<RedisModel> results = new List<RedisModel>();

            foreach (var key in keys)
            {
                var value = db.StringGet(key); // Assuming the values are stored as strings
                if (value.HasValue)
                {
                    RedisModel model = JsonSerializer.Deserialize<RedisModel>(value);
                    if (model != null)
                    {
                        results.Add(model);
                    }
                }
            }

            return Ok(results);
        }
    }

    [HttpGet("{id}", Name = "GetById")]
    public IActionResult GetById(string id)
    {
        var options = ConfigurationOptions.Parse("127.0.0.1:6379,allowAdmin=true");
        using (ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(options))
        {
            var server = redis.GetServer("127.0.0.1", 6379);
            var db = redis.GetDatabase();
            var result = db.StringGet(id);

            if (result == default)
            {
                return NotFound();
            }

            var resultDeserialized = JsonSerializer.Deserialize<RedisModel>(result);

            return Ok(resultDeserialized);
        }
    }

    [HttpPost]
    public IActionResult Create(RedisModel model)
    {
        var options = ConfigurationOptions.Parse("127.0.0.1:6379,allowAdmin=true");
        using (var redis = ConnectionMultiplexer.Connect(options))
        {
            var db = redis.GetDatabase();

            var plat = db.StringSet(model.Id, JsonSerializer.Serialize(model));

            return CreatedAtRoute(nameof(GetById), new { id = model.Id }, model);
        }
    }

    [HttpPut("{id}")]
    public IActionResult Update(string id, [FromBody] RedisModel model)
    {
        if (model == null || id != model.Id)
        {
            return BadRequest();
        }

        var options = ConfigurationOptions.Parse("127.0.0.1:6379,allowAdmin=true");
        using (var redis = ConnectionMultiplexer.Connect(options))
        {
            var db = redis.GetDatabase();
            //var key = $"model:{id}";
            var exists = db.KeyExists(id);
            if (!exists)
            {
                return NotFound();
            }

            var value = JsonSerializer.Serialize(model);
            db.StringSet(id, value);

            return Ok(model);
        }
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        var options = ConfigurationOptions.Parse("127.0.0.1:6379,allowAdmin=true");
        using (var redis = ConnectionMultiplexer.Connect(options))
        {
            var db = redis.GetDatabase();
            var deleted = db.KeyDelete(id);

            if (!deleted) return NotFound();

            return Ok();
        }
    }
}