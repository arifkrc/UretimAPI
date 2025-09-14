namespace UretimAPI.Extensions
{
    public static class SecurityExtensions
    {
        public static IServiceCollection AddCustomSecurity(this IServiceCollection services, IConfiguration configuration)
        {
            // Add request/response logging
            services.AddHttpLogging(logging =>
            {
                logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
                logging.RequestHeaders.Add("X-API-Version");
                logging.ResponseHeaders.Add("X-Total-Count");
                logging.MediaTypeOptions.AddText("application/json");
                logging.RequestBodyLogLimit = 4096;
                logging.ResponseBodyLogLimit = 4096;
            });

            return services;
        }

        public static WebApplication UseCustomSecurity(this WebApplication app)
        {
            // Security headers
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.Add("X-API-Version", "1.0");
                context.Response.Headers.Add("X-Powered-By", ""); // Remove server info
                
                // Add request ID for tracking
                if (!context.Response.Headers.ContainsKey("X-Request-ID"))
                {
                    context.Response.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());
                }

                await next();
            });

            return app;
        }
    }
}