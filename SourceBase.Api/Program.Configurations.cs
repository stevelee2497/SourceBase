using System.Text.Json.Nodes;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Enrichers.Span;
using SourceBase.Api.Middlewares;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Infrastructure.Hubs;

namespace SourceBase.Api;

public static class ProgramConfigurations
{
    public static void AddAppSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration);
        services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<AppSettings>>().Value);
    }

    public static void AddMvcConfigs(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.DefaultIgnoreCondition = Utilities.JsonOptions.DefaultIgnoreCondition;
            options.SerializerOptions.DictionaryKeyPolicy = Utilities.JsonOptions.DictionaryKeyPolicy;
            foreach (var converter in Utilities.JsonOptions.Converters)
            {
                options.SerializerOptions.Converters.Add(converter);
            }
        });
    }

    public static void AddCorsPolicies(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection(nameof(AppSettings.AllowedSpecificOrigins)).Get<string[]>() ?? [];
        services.AddCors(options =>
        {
            options.AddPolicy(Constants.CorsDefaultPolicy, builder =>
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());

            options.AddPolicy(Constants.CorsCustomPolicy, builder =>
                builder.WithOrigins(corsSettings)
                    .AllowAnyMethod()
                    .AllowAnyHeader());
        });
    }

    public static void AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });

            c.AddSecurityDefinition("BearerAuth", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme.",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
            });

            c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("BearerAuth", document, null), []
                }
            });

            c.DescribeAllParametersInCamelCase();

            c.MapType<TimeOnly>(() => new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "time",
                Example = JsonValue.Create("14:30")
            });
        });
    }

    public static void AddSeriLog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, logConfig) =>
        {
            logConfig
                .ReadFrom.Configuration(ctx.Configuration)
                .Enrich.WithSpan();
        });
    }

    public static void UseSeriLog(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
            };
        });
    }

    public static void MapMinimalApi(this WebApplication app)
    {
        app.MapGroup("/api").RequireAuthorization().RequireRateLimiting(Constants.GeneralRateLimitPolicy).AddEndpointFilter<ValidationEndpointFilter>().MapEndpoints(app);
    }

    public static void MapEndpoints(this IEndpointRouteBuilder builder, WebApplication app)
    {
        var endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(builder);
        }
    }

    public static void MapSignalR(this WebApplication app)
    {
        app.MapHub<NotificationHub>("/hubs/notifications");
    }

    public static void AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(Constants.GeneralRateLimitPolicy, httpContext =>
            {
                var settings = httpContext.RequestServices.GetRequiredService<AppSettings>().RateLimitSettings;
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.GetClientIp(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.GeneralPermitLimit,
                        Window = TimeSpan.FromSeconds(settings.GeneralWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    });
            });

            options.AddPolicy(Constants.StrictRateLimitPolicy, httpContext =>
            {
                var settings = httpContext.RequestServices.GetRequiredService<AppSettings>().RateLimitSettings;
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.GetClientIp(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.StrictPermitLimit,
                        Window = TimeSpan.FromSeconds(settings.StrictWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    });
            });

            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    TraceId = context.HttpContext.TraceIdentifier,
                    Code = "RATE_LIMIT_EXCEEDED",
                    Message = "Too many requests. Please try again later.",
                    Errors = new { }
                }, ct);
            };
        });


        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }
}
