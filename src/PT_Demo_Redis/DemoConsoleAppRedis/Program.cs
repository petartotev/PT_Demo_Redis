using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace DemoConsoleAppRedis;

public class Program
{
    private const string RedisConnectionString = "localhost:6379";

    public static void Main()
    {
        var serviceProvider = new ServiceCollection()
            .AddStackExchangeRedisCache(options => options.Configuration = RedisConnectionString)
            .BuildServiceProvider();

        var distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();
        var connectionMultiplexer = ConnectionMultiplexer.Connect(RedisConnectionString);
        var server = connectionMultiplexer.GetServer(RedisConnectionString);

        var eventId = 800001000;
        var deduplicationKey = $"Event_{eventId}";

        // SET KEY-VALUE
        distributedCache.SetString(deduplicationKey, "N/A");
        Console.WriteLine($"Value set for key '{deduplicationKey}': N/A");

        // GET BY KEY
        var retrievedValue = distributedCache.GetString(deduplicationKey);
        Console.WriteLine($"Value retrieved for key '{deduplicationKey}': {retrievedValue}");

        // GET ALL KEYS
        var keys = server.Keys(pattern: "*").ToList();
        Console.WriteLine("All keys in Redis:");
        foreach (var key in keys) Console.WriteLine(key);

        // DELETE ALL KEYS
        foreach (var key in keys)
        {
            distributedCache.Remove(key);
            Console.WriteLine($"Key '{key}' has been removed.");
        }

        // FLUSH DB
        // StackExchange.Redis.RedisCommandException: 'This operation is not available unless admin mode is enabled: FLUSHDB'
        server.FlushDatabase();
        Console.WriteLine("All entities in Redis have been flushed.");

        // DISPOSE
        connectionMultiplexer.Dispose();
        serviceProvider.Dispose();
    }
}
