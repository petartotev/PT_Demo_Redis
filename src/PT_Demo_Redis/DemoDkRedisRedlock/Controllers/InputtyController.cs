using DemoDkRedisRedlock.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace DemoDkRedisRedlock.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InputtyController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<InputtyController> _logger;
        private readonly IDistributedCache _distributedCache;

        public InputtyController(
            IConfiguration configuration,
            ILogger<InputtyController> logger,
            IDistributedCache distributedCache)
        {
            _configuration = configuration;
            _logger = logger;
            _distributedCache = distributedCache;
        }

        [HttpPost(Name = "Create Inputty Word")]
        public async Task<InputtyResponse> GetAsync(InputtyRequest request)
        {
            if (_distributedCache.GetString(request.Word) == null)
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_configuration.GetValue<int>("RedisTimeToLiveMinutes"))
                };

                await _distributedCache.SetStringAsync(request.Word, request.Word, cacheOptions);

                return new InputtyResponse { IsAlreadyCached = false };
            }

            return new InputtyResponse { IsAlreadyCached = true };
        }
    }
}
