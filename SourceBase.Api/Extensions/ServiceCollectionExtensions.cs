using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
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

    public static void UseSeeding(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        db.Database.EnsureCreated();
        ApplicationDbContext.SeedData(db, config);
    }
}