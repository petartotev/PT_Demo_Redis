using DemoDkRedisRedlock.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;

namespace DemoDkRedisRedlock.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InputtyController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<InputtyController> _logger;
        private readonly IDistributedCache _distributedCache;
        private readonly RedLockFactory _redLockFactory;

        public InputtyController(
            IConfiguration configuration,
            ILogger<InputtyController> logger,
            IDistributedCache distributedCache,
            IConnectionMultiplexer redisConnection)
        {
            _configuration = configuration;
            _logger = logger;
            _distributedCache = distributedCache;
            _redLockFactory = RedLockFactory.Create(new List<RedLockMultiplexer> { redisConnection as ConnectionMultiplexer });
        }

        [HttpPost(Name = "Create Inputty Word")]
        public async Task<IActionResult> GetAsync(InputtyRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Word))
                return BadRequest("Request should be valid!");

            var hash = CalculateSha256Hash(request.Word);

            var lockExpiry = TimeSpan.FromMinutes(_configuration.GetValue<int>("RedisTimeToLiveMinutes"));
            var lockKey = $"lock:{request.Word}";

            await using (var redLock = await _redLockFactory.CreateLockAsync(lockKey, lockExpiry))
            {
                if (redLock.IsAcquired)
                {
                    try
                    {
                        var valueFromCache = _distributedCache.GetString(request.Word);
                        _logger.LogInformation("Value for key {Key} retrieved from cache: {Value}", request.Word, valueFromCache);

                        if (valueFromCache == null)
                        {
                            var cacheOptions = new DistributedCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_configuration.GetValue<int>("RedisTimeToLiveMinutes"))
                            };

                            await _distributedCache.SetStringAsync(request.Word, hash, cacheOptions);

                            return Ok(new InputtyResponse { IsAlreadyCached = false, AlreadyCachedValue = "NONE" });
                        }

                        return Ok(new InputtyResponse { IsAlreadyCached = true, AlreadyCachedValue = valueFromCache });
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, $"Internal Server Error! Exception: {ex.Message}");
                    }
                }
                else
                {
                    return StatusCode(500, $"Internal Server Error! Red lock cannot be acquired!");
                }
            }
        }

        private static string CalculateSha256Hash(string text)
        {
            using var hash = SHA256.Create();
            var byteArray = hash.ComputeHash(Encoding.UTF8.GetBytes(text));
            return Convert.ToBase64String(byteArray);
        }
    }
}
