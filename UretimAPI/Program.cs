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
using System.Text.RegularExpressions;

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

    // Load .env file if present (simple parser)
    var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (File.Exists(envFile))
    {
        try
        {
            foreach (var line in File.ReadAllLines(envFile))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                var idx = trimmed.IndexOf('=');
                if (idx <= 0) continue;
                var key = trimmed.Substring(0, idx).Trim();
                var value = trimmed.Substring(idx + 1).Trim();
                // Remove optional surrounding quotes
                if ((value.StartsWith("\"") && value.EndsWith("\"")) || (value.StartsWith("'") && value.EndsWith("'")))
                    value = value.Substring(1, value.Length - 2);

                // Prefer existing environment variables over .env
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load .env file");
        }
    }

    // Ensure configuration picks up environment variables and appsettings afterwards
    builder.Configuration.AddEnvironmentVariables();

    // Configuration
    builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection(ApiSettings.SectionName));
    builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection(DatabaseSettings.SectionName));
    builder.Services.Configure<PerformanceSettings>(builder.Configuration.GetSection(PerformanceSettings.SectionName));

    // Log which connection string / database name will be used (don't log full connection string)
    try
    {
        var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        string dbName = null;
        if (!string.IsNullOrEmpty(defaultConnection))
        {
            // parse tokens like "Database=..." or "Initial Catalog=..."
            var tokens = defaultConnection.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var t in tokens)
            {
                if (t.StartsWith("Database=", StringComparison.OrdinalIgnoreCase) || t.StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = t.Split('=', 2);
                    if (parts.Length == 2) dbName = parts[1];
                    break;
                }
            }
        }

        if (!string.IsNullOrEmpty(dbName))
            Log.Information("Configured EF default connection will use database: {Database}", dbName);
        else if (!string.IsNullOrEmpty(defaultConnection))
            Log.Information("Configured EF default connection string present but database name could not be parsed. Check configuration sources.");
        else
            Log.Warning("No DefaultConnection configured. EF will fail to connect unless overridden by environment or secret settings.");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to parse DefaultConnection for logging");
    }

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

    // Normalize connection string to ensure it targets UretimDB
    var configuredConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    var finalConnection = configuredConnection;
    try
    {
        if (!string.IsNullOrEmpty(configuredConnection))
        {
            // Parse and replace Database or Initial Catalog token to enforce UretimDB
            var tokens = configuredConnection.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            var hasDatabaseToken = false;
            for (int i = 0; i < tokens.Count; i++)
            {
                var t = tokens[i];
                if (t.StartsWith("Database=", StringComparison.OrdinalIgnoreCase) || t.StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                {
                    tokens[i] = "Database=UretimDB";
                    hasDatabaseToken = true;
                    break;
                }
            }

            if (!hasDatabaseToken)
            {
                // append Database token
                tokens.Add("Database=UretimDB");
            }

            finalConnection = string.Join(';', tokens) + ";";

            if (!configuredConnection.Equals(finalConnection, StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("Overriding configured DefaultConnection database to 'UretimDB' for runtime to ensure correct target.");
            }
        }
        else
        {
            Log.Warning("No DefaultConnection found in configuration; unable to enforce UretimDB.");
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to normalize DefaultConnection; falling back to configured value.");
        finalConnection = configuredConnection;
    }

    builder.Services.AddDbContext<UretimDbContext>(options =>
    {
        options.UseSqlServer(finalConnection, sqlOptions =>
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
            // If no specific origins configured -> allow any origin (development convenience)
            if (apiSettings.AllowedOrigins == null || apiSettings.AllowedOrigins.Length == 0)
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            }
            else
            {
                // Use explicit origins list
                policy.WithOrigins(apiSettings.AllowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      // Only allow credentials if explicit origins (not wildcard)
                      .AllowCredentials()
                      .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
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

    // Global permissive CORS middleware - allows all origins for all endpoints
    app.Use(async (context, next) =>
    {
        context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, PATCH, DELETE, OPTIONS";
        context.Response.Headers["Access-Control-Allow-Headers"] = "*";

        if (string.Equals(context.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 200;
            await context.Response.CompleteAsync();
            return;
        }

        await next();
    });

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
