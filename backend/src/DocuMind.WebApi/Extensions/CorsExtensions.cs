using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.WebApi.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "DocuMindCors";

    public static IServiceCollection AddDocuMindCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy.WithOrigins(
                        "http://localhost:5173",
                        "http://localhost:3000",
                        "http://127.0.0.1:5173")
                      .WithMethods("GET", "POST", "DELETE", "OPTIONS")
                      .WithHeaders("Content-Type", "Accept", "Authorization")
                      .AllowCredentials();
            });
        });

        return services;
    }
}
