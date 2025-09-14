using Microsoft.Extensions.Caching.Memory;
using UretimAPI.DTOs.Common;

namespace UretimAPI.Services.Caching
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
        Task RemoveAsync(string key);
        Task RemoveByPatternAsync(string pattern);
    }

    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly HashSet<string> _cacheKeys;

        public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
            _cacheKeys = new HashSet<string>();
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                if (_cache.TryGetValue(key, out T? value))
                {
                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    return value;
                }
                
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(15),
                    SlidingExpiration = TimeSpan.FromMinutes(5),
                    Priority = CacheItemPriority.Normal
                };

                options.RegisterPostEvictionCallback((cacheKey, cacheValue, reason, state) =>
                {
                    _cacheKeys.Remove(cacheKey.ToString() ?? string.Empty);
                    _logger.LogDebug("Cache entry evicted: {Key}, Reason: {Reason}", cacheKey, reason);
                });

                _cache.Set(key, value, options);
                _cacheKeys.Add(key);
                
                _logger.LogDebug("Cache set for key: {Key}", key);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                _cache.Remove(key);
                _cacheKeys.Remove(key);
                _logger.LogDebug("Cache removed for key: {Key}", key);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache for key: {Key}", key);
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                var keysToRemove = _cacheKeys.Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();
                
                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                    _cacheKeys.Remove(key);
                }
                
                _logger.LogDebug("Cache cleared for pattern: {Pattern}, Keys removed: {Count}", pattern, keysToRemove.Count);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache by pattern: {Pattern}", pattern);
            }
        }
    }

    // Cache key generator for consistent naming
    public static class CacheKeys
    {
        public static string Products(int pageNumber, int pageSize, string? searchTerm = null) =>
            $"products:page:{pageNumber}:size:{pageSize}:search:{searchTerm ?? "all"}";

        public static string ProductById(int id) => $"product:id:{id}";
        public static string ProductByCode(string code) => $"product:code:{code}";
        
        public static string Operations(int pageNumber, int pageSize, string? searchTerm = null) =>
            $"operations:page:{pageNumber}:size:{pageSize}:search:{searchTerm ?? "all"}";

        public static string OperationById(int id) => $"operation:id:{id}";
        public static string OperationByShortCode(string shortCode) => $"operation:shortcode:{shortCode}";

        public static string ProductionTrackingByDateRange(DateTime startDate, DateTime endDate) =>
            $"ptf:daterange:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";

        public static string PackingByDateRange(DateTime startDate, DateTime endDate) =>
            $"packing:daterange:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";

        public static string OrdersByWeek(string week) => $"orders:week:{week}";
        public static string OrdersByCustomer(string customer) => $"orders:customer:{customer}";
    }
}