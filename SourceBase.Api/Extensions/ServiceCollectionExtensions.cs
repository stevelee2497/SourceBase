using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Serilog;
using SourceBase.Api.Common;
using SourceBase.Api.Infrastructure.DbContexts;

namespace SourceBase.Api.Extensions;

public static class ServiceCollectionExtensions
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
                    new OpenApiSecuritySchemeReference("BearerAuth", document, null),
                    []
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

    public static void UseSeriLog(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
        builder.Host.UseSerilog();
    }

    public static void UseSeeding(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        db.Database.EnsureCreated();
        ApplicationDbContext.SeedData(db, config);
    }

    public static void UseMinimalApi(this WebApplication app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();
        api.MapFeatureEndpoints(typeof(Program).Assembly);
    }

    public static IEndpointRouteBuilder MapFeatureEndpoints(this IEndpointRouteBuilder endpoints, Assembly assembly)
    {
        var endpointMethods = assembly.DefinedTypes
            .Where(type => type.Namespace?.StartsWith("SourceBase.Api.Features.", StringComparison.Ordinal) == true)
            .SelectMany(type => type.DeclaredMethods)
            .Where(method => method.IsPublic && method.IsStatic)
            .Where(method => method.Name.StartsWith("Map", StringComparison.Ordinal) && method.Name.EndsWith("Endpoint", StringComparison.Ordinal))
            .Where(HasValidEndpointSignature)
            .OrderBy(method => method.DeclaringType?.FullName, StringComparer.Ordinal)
            .ThenBy(method => method.Name, StringComparer.Ordinal);

        foreach (var endpointMethod in endpointMethods)
        {
            endpointMethod.Invoke(null, [endpoints]);
        }

        return endpoints;
    }

    private static bool HasValidEndpointSignature(MethodInfo method)
    {
        var parameters = method.GetParameters();
        return parameters.Length == 1 && typeof(IEndpointRouteBuilder).IsAssignableFrom(parameters[0].ParameterType);
    }
}
