using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using SourceBase.Api.Entities;
using SourceBase.Api.Infrastructure.DbContexts;
using SourceBase.Api.Infrastructure.Implementations;
using SourceBase.Api.Middlewares;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api;

public static class ProgramConfigurations
{
    public static void AddAppSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));
        services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<AppSettings>>().Value);
    }

    public static void AddMvcConfigs(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new TrimmingJsonConverter());
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        });
    }

    public static void AddCorsPolicies(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection(Constants.CorsCustomPolicy).Get<string[]>() ?? [];
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
        var logConfig = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration);

        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            logConfig = logConfig.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = otlpEndpoint;
                options.Protocol = OtlpProtocol.Grpc;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = builder.Configuration["OTEL_SERVICE_NAME"] ?? builder.Configuration["ApplicationName"] ?? "SourceBase.Api",
                    ["service.version"] = builder.Configuration["OTEL_SERVICE_VERSION"] ?? "1.0.0",
                };
            });
        }

        Log.Logger = logConfig.CreateLogger();
        builder.Host.UseSerilog();
    }

    public static void UseMinimalApi(this WebApplication app)
    {
        app.MapGroup("/api").RequireAuthorization().AddEndpointFilter<ValidationEndpointFilter>().MapEndpoints(app);
    }

    public static void MapEndpoints(this IEndpointRouteBuilder builder, WebApplication app)
    {
        var endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(builder);
        }
    }

    public static void AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var serviceDescriptors = assembly
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))
            .ToArray();

        services.TryAddEnumerable(serviceDescriptors);
    }

    public static void AddHandlers(this IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)));

        foreach (var type in handlerTypes)
            services.AddTransient(type, type);
    }

    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString)
                .UseSeeding((context, _) => ApplicationDbContext.SeedData(context, configuration))
                .UseAsyncSeeding(async (context, _, _) => ApplicationDbContext.SeedData(context, configuration));
        });
        services.AddScoped<IPasswordHasher<UserEntity>, PasswordHasher<UserEntity>>();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = Constants.BearerScheme;
                options.DefaultChallengeScheme = Constants.BearerScheme;
                options.DefaultForbidScheme = Constants.BearerScheme;
            })
            .AddBearerToken(Constants.BearerScheme, options =>
            {
                options.Events = new BearerTokenEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            context.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddSignalR();
        services.AddScoped<ISecurityProvider, SecurityProvider>();
        services.AddScoped<IDbContext, ApplicationDbContext>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IEmailHelper, SendGridEmailHelper>();
        services.AddScoped<INotificationService, NotificationService>();
    }

    public static void AddFluentValidation(this IServiceCollection services, Assembly assembly)
    {
        services.AddValidatorsFromAssembly(assembly);
    }

    public static void EnsureDatabaseMigrated(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
    }
}
