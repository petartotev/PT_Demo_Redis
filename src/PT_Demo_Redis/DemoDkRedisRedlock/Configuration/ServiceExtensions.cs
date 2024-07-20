using StackExchange.Redis;

namespace DemoDkRedisRedlock.Configuration
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddRedis(
            this IServiceCollection serviceCollection,
            IConfiguration configuration)
        {
            var redisConnectionString = configuration.GetValue<string>("RedisConnectionString");

            serviceCollection.AddStackExchangeRedisCache(opt => opt.Configuration = redisConnectionString);

            var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);

            serviceCollection.AddSingleton<IConnectionMultiplexer>(redisConnection);

            return serviceCollection;
        }
    }
}
