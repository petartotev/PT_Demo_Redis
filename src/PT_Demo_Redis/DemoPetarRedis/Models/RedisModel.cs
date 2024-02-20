using System.ComponentModel.DataAnnotations;

namespace DemoPetarRedis.Models;

public class RedisModel
{
    [Required]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int Age { get; set; }
}