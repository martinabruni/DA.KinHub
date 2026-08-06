using System.Collections.Concurrent;

namespace DA.KinHub.Functions.Functions;

public sealed class JoinFamilyRateLimiter(TimeProvider timeProvider)
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);
    private const int PerIdentityLimit = 5;
    private const int PerOriginLimit = 20;
    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> attempts = new(StringComparer.Ordinal);
    private readonly Lock gate = new();

    public bool TryAcquire(string identityKey, string originKey, out int retryAfterSeconds)
    {
        var nowUtc = timeProvider.GetUtcNow();
        lock (gate)
        {
            var identityQueue = GetQueue($"identity:{identityKey}");
            var originQueue = GetQueue($"origin:{originKey}");
            Trim(identityQueue, nowUtc);
            Trim(originQueue, nowUtc);

            if (identityQueue.Count >= PerIdentityLimit || originQueue.Count >= PerOriginLimit)
            {
                var retryAfter = TimeSpan.Zero;
                if (identityQueue.Count >= PerIdentityLimit)
                {
                    retryAfter = Window - (nowUtc - identityQueue.Peek());
                }

                if (originQueue.Count >= PerOriginLimit)
                {
                    var originRetryAfter = Window - (nowUtc - originQueue.Peek());
                    if (originRetryAfter > retryAfter)
                    {
                        retryAfter = originRetryAfter;
                    }
                }

                retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                return false;
            }

            identityQueue.Enqueue(nowUtc);
            originQueue.Enqueue(nowUtc);
            retryAfterSeconds = 0;
            return true;
        }
    }

    private Queue<DateTimeOffset> GetQueue(string key) => attempts.GetOrAdd(key, static _ => new Queue<DateTimeOffset>());

    private static void Trim(Queue<DateTimeOffset> queue, DateTimeOffset nowUtc)
    {
        while (queue.Count > 0 && nowUtc - queue.Peek() >= Window)
        {
            queue.Dequeue();
        }
    }
}
