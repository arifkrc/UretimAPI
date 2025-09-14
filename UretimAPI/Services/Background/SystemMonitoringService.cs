using Serilog;
using UretimAPI.Middleware;

namespace UretimAPI.Services.Background
{
    public class SystemMonitoringService : BackgroundService
    {
        private readonly ILogger<SystemMonitoringService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _monitoringInterval = TimeSpan.FromMinutes(15); // More frequent monitoring
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
        private DateTime _lastCleanup = DateTime.UtcNow;

        public SystemMonitoringService(ILogger<SystemMonitoringService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Enhanced System Monitoring Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MonitorSystemResourcesAsync();
                    
                    // Cleanup rate limiting data periodically
                    if (DateTime.UtcNow - _lastCleanup > _cleanupInterval)
                    {
                        RateLimitingMiddleware.CleanupOldClients();
                        _lastCleanup = DateTime.UtcNow;
                        _logger.LogDebug("Rate limiting cleanup completed");
                    }
                    
                    await Task.Delay(_monitoringInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during system monitoring");
                    await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                }
            }

            _logger.LogInformation("Enhanced System Monitoring Service stopped");
        }

        private async Task MonitorSystemResourcesAsync()
        {
            try
            {
                // Enhanced memory monitoring for extreme load
                var memoryUsage = GC.GetTotalMemory(false);
                var memoryUsageMB = memoryUsage / 1024 / 1024;

                // Process metrics
                using var process = System.Diagnostics.Process.GetCurrentProcess();
                var workingSetMB = process.WorkingSet64 / 1024 / 1024;
                var privateMemoryMB = process.PrivateMemorySize64 / 1024 / 1024;
                var handleCount = process.HandleCount;
                var threadCount = process.Threads.Count;

                // Database connection monitoring
                var dbConnectionsInfo = await MonitorDatabaseConnectionsAsync();
                
                // Performance counters
                var performanceMetrics = await GetPerformanceMetricsAsync();

                // Critical thresholds for extreme capacity (2x normal)
                var memoryThresholdMB = 1000; // 1GB threshold for extreme load
                var workingSetThresholdMB = 1200; // 1.2GB working set threshold
                var handleThreshold = 2000; // Handle count threshold
                var threadThreshold = 100; // Thread count threshold

                var shouldLogDetails = memoryUsageMB > memoryThresholdMB || 
                                     workingSetMB > workingSetThresholdMB ||
                                     handleCount > handleThreshold ||
                                     threadCount > threadThreshold ||
                                     DateTime.Now.Minute % 30 == 0; // Every 30 minutes

                if (shouldLogDetails)
                {
                    _logger.LogInformation("System Status - Memory: {MemoryMB}MB, WorkingSet: {WorkingSetMB}MB, " +
                        "PrivateMemory: {PrivateMemoryMB}MB, Handles: {HandleCount}, Threads: {ThreadCount}, " +
                        "DB: {DbStatus}, Performance: {Performance}",
                        memoryUsageMB, workingSetMB, privateMemoryMB, handleCount, threadCount, 
                        dbConnectionsInfo, performanceMetrics);
                }

                // Critical alerts for extreme situations
                if (memoryUsageMB > memoryThresholdMB)
                {
                    _logger.LogWarning("CRITICAL: High memory usage detected: {MemoryMB}MB (Threshold: {ThresholdMB}MB)", 
                        memoryUsageMB, memoryThresholdMB);
                    
                    // Aggressive garbage collection for extreme load
                    for (int i = 0; i < 3; i++)
                    {
                        GC.Collect(2, GCCollectionMode.Forced);
                        GC.WaitForPendingFinalizers();
                    }
                    
                    var memoryAfterGC = GC.GetTotalMemory(true) / 1024 / 1024;
                    _logger.LogInformation("Memory after aggressive GC: {MemoryAfterGC}MB", memoryAfterGC);
                }

                if (workingSetMB > workingSetThresholdMB)
                {
                    _logger.LogError("CRITICAL: Working set too high: {WorkingSetMB}MB - System may need restart", workingSetMB);
                }

                if (handleCount > handleThreshold || threadCount > threadThreshold)
                {
                    _logger.LogWarning("Resource usage warning - Handles: {HandleCount}, Threads: {ThreadCount}", 
                        handleCount, threadCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring system resources");
            }
        }

        private async Task<string> MonitorDatabaseConnectionsAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Data.UretimDbContext>();
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var canConnect = await dbContext.Database.CanConnectAsync();
                stopwatch.Stop();
                
                var responseTime = stopwatch.ElapsedMilliseconds;
                
                if (!canConnect)
                    return "Disconnected";
                    
                if (responseTime > 5000) // 5 seconds
                    return $"Slow ({responseTime}ms)";
                    
                if (responseTime > 1000) // 1 second
                    return $"Warning ({responseTime}ms)";
                    
                return $"Healthy ({responseTime}ms)";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database connection check failed");
                return "Error";
            }
        }

        private async Task<string> GetPerformanceMetricsAsync()
        {
            try
            {
                // Simple performance indicators
                var gen0Collections = GC.CollectionCount(0);
                var gen1Collections = GC.CollectionCount(1);
                var gen2Collections = GC.CollectionCount(2);
                
                return $"GC(G0:{gen0Collections},G1:{gen1Collections},G2:{gen2Collections})";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance metrics");
                return "Error";
            }
        }
    }
}