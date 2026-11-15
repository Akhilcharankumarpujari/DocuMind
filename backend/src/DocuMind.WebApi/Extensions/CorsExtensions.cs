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
                policy.SetIsOriginAllowed(origin =>
                      {
                          if (string.IsNullOrWhiteSpace(origin)) return false;
                          if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                          {
                              return uri.Host is "localhost" or "127.0.0.1" or "0.0.0.0";
                          }
                          return false;
                      })
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }
}
