using Microsoft.AspNetCore.Mvc;
using DemoPetarRedis.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace DemoPetarRedis.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RedisController : ControllerBase
{
    private readonly IConnectionMultiplexer _multiplex;
    private readonly IDatabase _database;

    public RedisController(IConnectionMultiplexer multiplex)
    {
        _multiplex = multiplex;
        _database = _multiplex.GetDatabase();
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var server = _multiplex.GetServer(_multiplex.GetEndPoints().First());
        var keys = server.Keys(pattern: "*").ToArray(); // Use with caution; consider specifying a more specific pattern
        var results = new List<RedisModel>();

        foreach (var key in keys)
        {
            var value = _database.StringGet(key); // Assuming the values are stored as strings
            if (value.HasValue)
            {
                var model = JsonSerializer.Deserialize<RedisModel>(value);
                if (model != null)
                    results.Add(model);
            }
        }

        return Ok(results);
    }

    [HttpGet("{id}", Name = "GetById")]
    public IActionResult GetById(string id)
    {
        var result = _database.StringGet(id);

        if (result.IsNull)
            return NotFound();

        return Ok(JsonSerializer.Deserialize<RedisModel>(result));
    }

    [HttpPost]
    public IActionResult Create(RedisModel model)
    {
        var success = _database.StringSet(model.Id, JsonSerializer.Serialize(model), when: When.NotExists);

        if (!success)
            return StatusCode(409, "A model with the same ID already exists.");

        return CreatedAtRoute(nameof(GetById), new { id = model.Id }, model);
    }

    [HttpPut("{id}")]
    public IActionResult Update(string id, [FromBody] RedisModel model)
    {
        if (model == null || id != model.Id)
            return BadRequest("Model is null or ID mismatch.");

        if (!_database.KeyExists(id))
            return NotFound();

        _database.StringSet(id, JsonSerializer.Serialize(model));

        return Ok(model);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        if (!_database.KeyDelete(id))
            return NotFound();

        return Ok();
    }
}