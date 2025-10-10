using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Serilog;
using SourceBase.Api.Filters;
using SourceBase.Application.Common;
using SourceBase.Application.Features.Auth;
using SourceBase.Domain.Entities;
using SourceBase.Infrastructure.DbContexts;
using System.Reflection;
using System.Text.Json.Serialization;

namespace SourceBase.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddAppSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));
        services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<AppSettings>>().Value);
    }

    public static void AddApplicationDbContext(this IServiceCollection services)
    {
        // Add EF Business
        services.AddDbContext<ApplicationDbContext>();

        // Add EF Identity Dependencies
        services.AddIdentityApiEndpoints<UserEntity>()          // Set up Identity managers and stores
            .AddRoles<RoleEntity>()                             // Set up Role-based manager and store
            .AddEntityFrameworkStores<ApplicationDbContext>();  // Attach Identity to our DB context
    }

    public static void AddMvcConfigs(this IServiceCollection services)
    {
        services
            .AddControllers(options =>
            {
                options.Filters.Add<AuthorizationFilter>();
                options.Filters.Add<ExceptionFilter>();                     // Add global exception filter to force all exceptions into our error model
                options.Filters.Add<ModelValidationFilter>(int.MinValue);   // Validating json payload and return in error model format
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); // Force to save enum in string format to our database instead of magic numbers
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
    }

    public static void AddDependencyInjections(this IServiceCollection services)
    {
        //Http Context
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Action Context Accessor
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

        var assemblies = new[] { typeof(Program).Assembly, typeof(AuthService).Assembly, typeof(BaseEntity).Assembly, typeof(ApplicationDbContext).Assembly };
        var implementations = assemblies.SelectMany(a => a.GetTypes().Where(x => x.GetCustomAttributes<DependencyAttribute>().Any())) ?? [];

        foreach (var implementationType in implementations)
        {
            var attribute = implementationType.GetCustomAttributes<DependencyAttribute>().FirstOrDefault() ?? throw new ApiInternalException();

            switch (attribute.ServiceLifeTime)
            {
                case ServiceLifetime.Transient:
                    services.AddTransient(attribute.ServiceType, implementationType);
                    break;
                case ServiceLifetime.Scoped:
                    services.AddScoped(attribute.ServiceType, implementationType);
                    break;
                case ServiceLifetime.Singleton:
                    services.AddSingleton(attribute.ServiceType, implementationType);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
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

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "BearerAuth"
                        }
                    },
                    []
                }
            });

            c.DescribeAllParametersInCamelCase();

            c.MapType<TimeOnly>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "time",
                Example = new OpenApiString("14:30")
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
}