using Microsoft.EntityFrameworkCore;
using UretimAPI.Data;
using UretimAPI.Repositories.Interfaces;
using UretimAPI.Repositories.Implementations;
using UretimAPI.Services.Interfaces;
using UretimAPI.Services.Implementations;
using UretimAPI.Services.Background;
using UretimAPI.Services.Caching;
using UretimAPI.Configuration;
using UretimAPI.Middleware;
using System.Text.Json.Serialization;
using Serilog;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog
    builder.Host.UseSerilog();

    // Configuration
    builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection(ApiSettings.SectionName));
    builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection(DatabaseSettings.SectionName));
    builder.Services.Configure<PerformanceSettings>(builder.Configuration.GetSection(PerformanceSettings.SectionName));

    // Add services to the container with enhanced configuration
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            // Performance optimizations for extreme load
            options.JsonSerializerOptions.WriteIndented = false;
            options.JsonSerializerOptions.MaxDepth = 32;
        });

    // Entity Framework DbContext with extreme load configuration
    var databaseSettings = builder.Configuration.GetSection(DatabaseSettings.SectionName).Get<DatabaseSettings>() ?? new DatabaseSettings();

    builder.Services.AddDbContext<UretimDbContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
        {
            sqlOptions.CommandTimeout(databaseSettings.CommandTimeoutSeconds);
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: databaseSettings.MaxRetryCount,
                maxRetryDelay: TimeSpan.FromSeconds(databaseSettings.MaxRetryDelay),
                errorNumbersToAdd: null);
            
            // Extreme load optimizations
            sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });

        // Enhanced EF configuration for extreme load
        options.EnableServiceProviderCaching();
        options.EnableSensitiveDataLogging(false); // Always false for performance
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking); // Default to no tracking for reads
        
        if (builder.Environment.IsDevelopment())
        {
            options.EnableDetailedErrors(databaseSettings.EnableDetailedErrors);
            options.LogTo(message => Log.Debug("EF: {Message}", message), Microsoft.Extensions.Logging.LogLevel.Information);
        }
    });

    // AutoMapper
    builder.Services.AddAutoMapper(typeof(Program));

    // Repository Pattern
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<IOperationRepository, OperationRepository>();
    builder.Services.AddScoped<ICycleTimeRepository, CycleTimeRepository>();
    builder.Services.AddScoped<IProductionTrackingFormRepository, ProductionTrackingFormRepository>();
    builder.Services.AddScoped<IPackingRepository, PackingRepository>();
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();

    // Service Pattern
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<IOperationService, OperationService>();
    builder.Services.AddScoped<ICycleTimeService, CycleTimeService>();
    builder.Services.AddScoped<IProductionTrackingFormService, ProductionTrackingFormService>();
    builder.Services.AddScoped<IPackingService, PackingService>();
    builder.Services.AddScoped<IOrderService, OrderService>();

    // Caching Service
    builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

    // Background Services
    builder.Services.AddHostedService<SystemMonitoringService>();

    // Enhanced Memory Caching for extreme load
    builder.Services.AddMemoryCache(options =>
    {
        options.SizeLimit = 512 * 1024 * 1024; // 512MB cache limit
        options.CompactionPercentage = 0.25; // Remove 25% when limit reached
        options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
    });

    // Response compression for better performance
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    });

    // CORS Configuration
    var apiSettings = builder.Configuration.GetSection(ApiSettings.SectionName).Get<ApiSettings>() ?? new ApiSettings();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultPolicy", policy =>
        {
            policy.WithOrigins(apiSettings.AllowedOrigins.Any() ? apiSettings.AllowedOrigins : new[] { "*" })
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(10)); // Cache preflight requests
            
            if (!apiSettings.AllowedOrigins.Any())
            {
                policy.AllowAnyOrigin();
            }
            else
            {
                policy.AllowCredentials();
            }
        });
    });

    // Enhanced Health Checks for extreme capacity
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<UretimDbContext>("database")
        .AddCheck("memory", () => 
        {
            var workingSet = GC.GetTotalMemory(false);
            var workingSetMB = workingSet / 1024 / 1024;
            
            // Extreme capacity thresholds
            if (workingSetMB > 1000) // 1GB
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"Memory too high: {workingSetMB} MB");
            
            if (workingSetMB > 750) // 750MB warning
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded($"Memory high: {workingSetMB} MB");
                
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"Memory: {workingSetMB} MB");
        })
        .AddCheck("disk-space", () =>
        {
            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(Directory.GetCurrentDirectory()) ?? "C:");
                var freeSpaceGB = driveInfo.AvailableFreeSpace / (1024L * 1024L * 1024L);
                
                if (freeSpaceGB < 1) // Less than 1GB
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"Low disk space: {freeSpaceGB} GB");
                
                if (freeSpaceGB < 5) // Less than 5GB warning
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded($"Disk space warning: {freeSpaceGB} GB");
                    
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"Disk space: {freeSpaceGB} GB");
            }
            catch (Exception ex)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"Disk check failed: {ex.Message}");
            }
        });

    // Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
        { 
            Title = "Uretim API", 
            Version = "v1",
            Description = "Production Tracking API for manufacturing operations - Optimized for extreme load (2x capacity)"
        });
        
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    var app = builder.Build();

    // Log application startup
    Log.Information("Starting Uretim API application with extreme load capacity");

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Uretim API v1");
            c.RoutePrefix = "swagger";
        });
    }

    // Response compression (before other middleware)
    app.UseResponseCompression();

    // Rate limiting middleware
    app.UseMiddleware<RateLimitingMiddleware>();

    // Global Exception Handling Middleware
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // Security Headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Add("X-API-Version", "1.0");
        await next();
    });

    app.UseHttpsRedirection();

    app.UseCors("DefaultPolicy");

    app.UseAuthorization();

    app.MapControllers();

    // Enhanced Health Checks endpoint
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready");
    app.MapHealthChecks("/health/live");

    Log.Information("Uretim API application started successfully with extreme load configuration");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
