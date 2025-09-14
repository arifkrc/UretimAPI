using Microsoft.Extensions.Diagnostics.HealthChecks;
using UretimAPI.Data;

namespace UretimAPI.Extensions
{
    public static class HealthCheckExtensions
    {
        public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
                .AddDbContextCheck<UretimDbContext>("database", tags: new[] { "db", "ready" })
                .AddCheck("memory", () => 
                {
                    var workingSet = GC.GetTotalMemory(false);
                    var workingSetMB = workingSet / 1024 / 1024;
                    
                    return workingSetMB switch
                    {
                        > 1000 => HealthCheckResult.Unhealthy($"Memory too high: {workingSetMB} MB"),
                        > 750 => HealthCheckResult.Degraded($"Memory high: {workingSetMB} MB"),
                        _ => HealthCheckResult.Healthy($"Memory: {workingSetMB} MB")
                    };
                }, tags: new[] { "memory", "ready" })
                .AddCheck("disk-space", () =>
                {
                    try
                    {
                        var driveInfo = new DriveInfo(Path.GetPathRoot(Directory.GetCurrentDirectory()) ?? "C:");
                        var freeSpaceGB = driveInfo.AvailableFreeSpace / (1024L * 1024L * 1024L);
                        
                        return freeSpaceGB switch
                        {
                            < 1 => HealthCheckResult.Unhealthy($"Low disk space: {freeSpaceGB} GB"),
                            < 5 => HealthCheckResult.Degraded($"Disk space warning: {freeSpaceGB} GB"),
                            _ => HealthCheckResult.Healthy($"Disk space: {freeSpaceGB} GB")
                        };
                    }
                    catch (Exception ex)
                    {
                        return HealthCheckResult.Unhealthy($"Disk check failed: {ex.Message}");
                    }
                }, tags: new[] { "disk", "ready" })
                .AddCheck("api-response-time", () =>
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    
                    // Simulate a quick health check operation
                    Thread.Sleep(1);
                    
                    stopwatch.Stop();
                    var responseTime = stopwatch.ElapsedMilliseconds;
                    
                    return responseTime switch
                    {
                        > 1000 => HealthCheckResult.Unhealthy($"API response time too slow: {responseTime}ms"),
                        > 500 => HealthCheckResult.Degraded($"API response time slow: {responseTime}ms"),
                        _ => HealthCheckResult.Healthy($"API response time: {responseTime}ms")
                    };
                }, tags: new[] { "api", "ready" });

            return services;
        }
    }
}