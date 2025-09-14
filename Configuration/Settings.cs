namespace UretimAPI.Configuration
{
    public class ApiSettings
    {
        public const string SectionName = "ApiSettings";
        
        public int DefaultPageSize { get; set; } = 50; // 25'ten 50'ye art?r?ld?
        public int MaxPageSize { get; set; } = 200; // 100'den 200'e art?r?ld?
        public int BulkOperationLimit { get; set; } = 250; // 100'den 250'ye art?r?ld?
        public bool EnableDetailedErrors { get; set; } = false;
        public int CacheExpirationMinutes { get; set; } = 10; // 15'ten 10'a azalt?ld?
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
        
        // ?yile?tirilmi? daily limits
        public int DailyEntryLimit { get; set; } = 2000; // 500'den 2000'e art?r?ld?
        public int DailyQueryLimit { get; set; } = 2000; // 500'den 2000'e art?r?ld?
        public bool EnableQueryThrottling { get; set; } = true;
        public int QueryThrottleWindowMinutes { get; set; } = 15; // 60'tan 15'e azalt?ld?
        
        // Yeni performans ayarlar?
        public int MaxConcurrentUsers { get; set; } = 20;
        public int RequestTimeoutSeconds { get; set; } = 30;
    }

    public class DatabaseSettings
    {
        public const string SectionName = "DatabaseSettings";
        
        public int CommandTimeoutSeconds { get; set; } = 60; // 30'dan 60'a art?r?ld?
        public bool EnableSensitiveDataLogging { get; set; } = false;
        public bool EnableDetailedErrors { get; set; } = true; // Development için true
        public int MaxRetryCount { get; set; } = 5; // 3'ten 5'e art?r?ld?
        public int MaxRetryDelay { get; set; } = 60; // 30'dan 60'a art?r?ld?
        
        // Improved connection pool settings
        public int ConnectionPoolMaxSize { get; set; } = 50; // 10'dan 50'ye art?r?ld?
        public int ConnectionPoolMinSize { get; set; } = 5; // 2'den 5'e art?r?ld?
        public int QueryCacheSize { get; set; } = 500; // 100'den 500'e art?r?ld?
        public int BulkInsertBatchSize { get; set; } = 100;
        public int MaxDegreeOfParallelism { get; set; } = 4;
    }

    public class PerformanceSettings
    {
        public const string SectionName = "PerformanceSettings";
        
        public int MaxConcurrentRequests { get; set; } = 100; // 50'den 100'e art?r?ld?
        public int DatabaseConnectionPoolSize { get; set; } = 50; // 10'dan 50'ye art?r?ld?
        public bool EnableResponseCompression { get; set; } = true;
        public bool EnableOutputCaching { get; set; } = true;
        public int OutputCacheDurationSeconds { get; set; } = 600; // 300'den 600'e art?r?ld?
        public int MemoryCacheSizeLimitMB { get; set; } = 512;
        public bool EnableQueryOptimization { get; set; } = true;
        
        // Rate limiting settings
        public int MaxRequestsPerMinute { get; set; } = 120;
        public int MaxBulkOperationsPerHour { get; set; } = 20;
        public int RateLimitWindowMinutes { get; set; } = 1;
    }
}