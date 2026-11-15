using System.Text;
using System.Threading.RateLimiting;
using DocuMind.Application;
using DocuMind.Infrastructure;
using DocuMind.WebApi.Extensions;
using DocuMind.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
    Log.Information("Starting DocuMind.WebApi host with enterprise security...");

    // Add Services
    builder.Services.AddDocuMindCors();
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("auth-policy", opt =>
        {
            opt.PermitLimit = 15;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 2;
        });
    });

    // JWT Bearer Authentication
    var jwtSecret = builder.Configuration["Jwt:SecretKey"]
        ?? builder.Configuration["JWT_SECRET"]
        ?? "DocuMind-UltraSecure-JwtTokenSigningKey-2026-Enterprise!";

    var jwtIssuer = builder.Configuration["Jwt:Issuer"]
        ?? builder.Configuration["JWT_ISSUER"]
        ?? "DocuMind.WebApi";

    var jwtAudience = builder.Configuration["Jwt:Audience"]
        ?? builder.Configuration["JWT_AUDIENCE"]
        ?? "DocuMind.Client";

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Allow local HTTP development
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(5)
        };
    });

    builder.Services.AddAuthorization();

    builder.Services.AddControllers();
    builder.Services.AddHealthChecks();

    // Configure Swagger / OpenAPI with JWT Authorization Support
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "DocuMind API",
            Version = "v1",
            Description = "AI-Powered PDF Document Analyzer and RAG Chatbot Backend API with Multi-User Authentication"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT Bearer token: e.g. Bearer {your token}"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
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
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // Endpoints
    app.MapControllers();
    app.MapHealthChecks("/health");

    // Automatically apply EF Core database migrations on startup and safely preserve legacy documents
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DocuMind.Infrastructure.Persistence.DocuMindDbContext>();
            Log.Information("Checking and applying pending database migrations...");
            dbContext.Database.Migrate();
            Log.Information("Database schema is up to date.");

            // Safe migration strategy: Assign unassigned legacy documents to designated administrator
            var unownedDocs = dbContext.Documents.Where(d => d.UserId == null).ToList();
            if (unownedDocs.Count > 0)
            {
                Log.Information("Found {Count} legacy documents without owner. Ensuring designated migration admin user exists...", unownedDocs.Count);
                var adminEmail = Environment.GetEnvironmentVariable("MIGRATION_ADMIN_EMAIL") ?? "admin@documind.ai";
                var normalizedAdminEmail = adminEmail.Trim().ToUpperInvariant();

                var adminUser = dbContext.Users.FirstOrDefault(u => u.NormalizedEmail == normalizedAdminEmail);
                if (adminUser == null)
                {
                    var passwordHasher = scope.ServiceProvider.GetRequiredService<DocuMind.Application.Common.Interfaces.IPasswordHasher>();
                    var adminPassword = Environment.GetEnvironmentVariable("MIGRATION_ADMIN_PASSWORD") ?? "Admin@DocuMind2026!";
                    var adminPasswordHash = passwordHasher.HashPassword(adminPassword);
                    adminUser = new DocuMind.Domain.Entities.User("DocuMind Administrator", adminEmail, adminPasswordHash);
                    dbContext.Users.Add(adminUser);
                    dbContext.SaveChanges();
                    Log.Information("Designated migration administrator {AdminEmail} created successfully.", adminEmail);
                }

                foreach (var doc in unownedDocs)
                {
                    doc.UserId = adminUser.Id;
                }
                dbContext.SaveChanges();
                Log.Information("Assigned {Count} legacy documents to designated administrator {AdminEmail}.", unownedDocs.Count, adminEmail);
            }
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
                else if (key == "JWT_SECRET")
                {
                    config["Jwt:SecretKey"] = val;
                    Environment.SetEnvironmentVariable("Jwt__SecretKey", val);
                }
                else if (key == "JWT_ISSUER")
                {
                    config["Jwt:Issuer"] = val;
                    Environment.SetEnvironmentVariable("Jwt__Issuer", val);
                }
                else if (key == "JWT_AUDIENCE")
                {
                    config["Jwt:Audience"] = val;
                    Environment.SetEnvironmentVariable("Jwt__Audience", val);
                }
                else if (key == "JWT_ACCESS_TOKEN_MINUTES")
                {
                    config["Jwt:AccessTokenExpirationMinutes"] = val;
                    Environment.SetEnvironmentVariable("Jwt__AccessTokenExpirationMinutes", val);
                }
                else if (key == "JWT_REFRESH_TOKEN_DAYS")
                {
                    config["Jwt:RefreshTokenExpirationDays"] = val;
                    Environment.SetEnvironmentVariable("Jwt__RefreshTokenExpirationDays", val);
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
