namespace DemoDkRedisRedlock.Contracts
{
    public class InputtyResponse
    {
        public bool IsAlreadyCached { get; set; }
        public string AlreadyCachedValue { get; set; }
    }
}
