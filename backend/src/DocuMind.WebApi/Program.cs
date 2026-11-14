using DocuMind.Application;
using DocuMind.Infrastructure;
using DocuMind.WebApi.Extensions;
using DocuMind.WebApi.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

// Load local .env variables into environment if present
var dotEnvConfig = LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

if (dotEnvConfig.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(dotEnvConfig);
}

// Configure Serilog Logging
builder.ConfigureSerilog();

try
{
    Log.Information("Starting DocuMind.WebApi host...");

    // Add Services
    builder.Services.AddDocuMindCors();
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    builder.Services.AddControllers();
    builder.Services.AddHealthChecks();

    // Configure Swagger / OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "DocuMind API",
            Version = "v1",
            Description = "AI-Powered PDF Document Analyzer and RAG Chatbot Backend API"
        });
    });

    var app = builder.Build();

    // Global Exception & Diagnostics Middleware
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseSerilogRequestLogging();

    // Swagger UI Configuration
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DocuMind API v1");
        c.RoutePrefix = "swagger";
    });

    app.UseRouting();
    app.UseCors(CorsExtensions.PolicyName);

    // Endpoints
    app.MapControllers();
    app.MapHealthChecks("/health");

    // Automatically apply EF Core database migrations on startup
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DocuMind.Infrastructure.Persistence.DocuMindDbContext>();
            Log.Information("Checking and applying pending database migrations...");
            dbContext.Database.Migrate();
            Log.Information("Database schema is up to date.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not apply database migrations on startup. Ensure database is accessible.");
        }
    }

    Log.Information("DocuMind Web API configured and ready to accept requests on port 5000");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "DocuMind.WebApi host terminated unexpectedly.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static Dictionary<string, string?> LoadDotEnv()
{
    var config = new Dictionary<string, string?>();

    // Search current directory and up to 5 parent directories for .env or backend/.env
    var searchDirs = new List<string> { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };

    DirectoryInfo? dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null)
    {
        searchDirs.Add(dir.FullName);
        dir = dir.Parent;
    }

    string? resolvedEnvPath = null;
    foreach (var baseDir in searchDirs.Distinct())
    {
        var direct = Path.Combine(baseDir, ".env");
        var inBackend = Path.Combine(baseDir, "backend", ".env");

        if (File.Exists(direct))
        {
            resolvedEnvPath = direct;
            break;
        }
        if (File.Exists(inBackend))
        {
            resolvedEnvPath = inBackend;
            break;
        }
    }

    if (resolvedEnvPath != null)
    {
        foreach (var line in File.ReadAllLines(resolvedEnvPath))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("#") || !trimmed.Contains('=')) continue;
            var parts = trimmed.Split('=', 2);
            var key = parts[0].Trim();
            var val = parts[1].Trim().Trim('"').Trim('\'');

            if (!string.IsNullOrEmpty(key))
            {
                Environment.SetEnvironmentVariable(key, val);

                // Map Docker/env style to ASP.NET Core hierarchical configuration keys
                if (key == "AZURE_OPENAI_ENDPOINT")
                {
                    config["AzureOpenAi:Endpoint"] = val;
                    Environment.SetEnvironmentVariable("AzureOpenAi__Endpoint", val);
                }
                else if (key == "AZURE_OPENAI_API_KEY")
                {
                    config["AzureOpenAi:ApiKey"] = val;
                    Environment.SetEnvironmentVariable("AzureOpenAi__ApiKey", val);
                }
                else if (key == "AZURE_OPENAI_CHAT_DEPLOYMENT")
                {
                    config["AzureOpenAi:ChatDeploymentName"] = val;
                    Environment.SetEnvironmentVariable("AzureOpenAi__ChatDeploymentName", val);
                }
                else if (key == "AZURE_OPENAI_EMBEDDING_DEPLOYMENT")
                {
                    config["AzureOpenAi:EmbeddingDeploymentName"] = val;
                    Environment.SetEnvironmentVariable("AzureOpenAi__EmbeddingDeploymentName", val);
                }
            }
        }

        var dbUser = Environment.GetEnvironmentVariable("MYSQL_USER") ?? "documind_user";
        var dbPass = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");
        var dbHost = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "localhost";
        var dbPort = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3308";
        var dbName = Environment.GetEnvironmentVariable("MYSQL_DATABASE") ?? "documind_db";
        if (!string.IsNullOrEmpty(dbPass))
        {
            var connStr = $"Server={dbHost};Port={dbPort};Database={dbName};Uid={dbUser};Pwd={dbPass};";
            config["ConnectionStrings:DefaultConnection"] = connStr;
            config["Database:ConnectionString"] = connStr;
        }
    }

    return config;
}
