using System.Collections.Concurrent;
using System.Net;

namespace UretimAPI.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private static readonly ConcurrentDictionary<string, ClientRateLimit> _clients = new();
        private readonly RateLimitOptions _options;

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _options = new RateLimitOptions
            {
                MaxRequestsPerMinute = configuration.GetValue<int>("PerformanceSettings:MaxRequestsPerMinute", 60),
                MaxBulkOperationsPerHour = configuration.GetValue<int>("PerformanceSettings:MaxBulkOperationsPerHour", 10),
                WindowSizeMinutes = configuration.GetValue<int>("PerformanceSettings:RateLimitWindowMinutes", 1)
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientIdentifier(context);
            var isBulkOperation = IsBulkOperation(context);
            
            if (!await IsRequestAllowedAsync(clientId, isBulkOperation))
            {
                await HandleRateLimitExceeded(context, clientId, isBulkOperation);
                return;
            }

            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Use IP address as client identifier (in production, use user ID or API key)
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private bool IsBulkOperation(HttpContext context)
        {
            return context.Request.Path.Value?.Contains("bulk", StringComparison.OrdinalIgnoreCase) == true ||
                   context.Request.Method == "POST" && context.Request.ContentLength > 50000; // 50KB+ requests
        }

        private async Task<bool> IsRequestAllowedAsync(string clientId, bool isBulkOperation)
        {
            var now = DateTimeOffset.UtcNow;
            var client = _clients.GetOrAdd(clientId, _ => new ClientRateLimit());

            lock (client)
            {
                // Clean old requests outside the window
                var windowStart = now.AddMinutes(-_options.WindowSizeMinutes);
                client.Requests.RemoveAll(r => r < windowStart);
                client.BulkOperations.RemoveAll(r => r < now.AddHours(-1)); // 1 hour window for bulk ops

                // Check rate limits
                if (client.Requests.Count >= _options.MaxRequestsPerMinute)
                {
                    _logger.LogWarning("Rate limit exceeded for client {ClientId}: {RequestCount} requests in window", 
                        clientId, client.Requests.Count);
                    return false;
                }

                if (isBulkOperation && client.BulkOperations.Count >= _options.MaxBulkOperationsPerHour)
                {
                    _logger.LogWarning("Bulk operation rate limit exceeded for client {ClientId}: {BulkCount} operations in hour", 
                        clientId, client.BulkOperations.Count);
                    return false;
                }

                // Add current request
                client.Requests.Add(now);
                if (isBulkOperation)
                {
                    client.BulkOperations.Add(now);
                }
            }

            return true;
        }

        private async Task HandleRateLimitExceeded(HttpContext context, string clientId, bool isBulkOperation)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";

            var message = isBulkOperation 
                ? $"Bulk operation rate limit exceeded. Maximum {_options.MaxBulkOperationsPerHour} bulk operations per hour."
                : $"Request rate limit exceeded. Maximum {_options.MaxRequestsPerMinute} requests per minute.";

            var response = new
            {
                error = "Rate limit exceeded",
                message = message,
                clientId = clientId,
                timestamp = DateTimeOffset.UtcNow
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }

        // Cleanup old clients periodically
        public static void CleanupOldClients()
        {
            var cutoff = DateTimeOffset.UtcNow.AddHours(-2);
            var clientsToRemove = _clients
                .Where(kvp => kvp.Value.Requests.All(r => r < cutoff) && 
                             kvp.Value.BulkOperations.All(r => r < cutoff))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var clientId in clientsToRemove)
            {
                _clients.TryRemove(clientId, out _);
            }
        }
    }

    public class ClientRateLimit
    {
        public List<DateTimeOffset> Requests { get; } = new();
        public List<DateTimeOffset> BulkOperations { get; } = new();
    }

    public class RateLimitOptions
    {
        public int MaxRequestsPerMinute { get; set; } = 60;
        public int MaxBulkOperationsPerHour { get; set; } = 10;
        public int WindowSizeMinutes { get; set; } = 1;
    }
}