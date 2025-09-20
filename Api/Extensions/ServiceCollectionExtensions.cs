using Api.Contexts;
using Api.Filters;
using Application.Services;
using Domain.Contexts;
using Domain.Entities;
using Infrastructure.Contexts;
using System.Text.Json.Serialization;

namespace Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DB Context
        services.AddScoped<IDbContext, ApplicationDbContext>();
        services.AddScoped<IUserContext, IdentityUserContext>();

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
                options.Filters.Add<ApiAuthorizationFilter>();
                options.Filters.Add<ExceptionFilter>();                     // Add global exception filter to force all exceptions into our error model
                options.Filters.Add<ModelValidationFilter>(int.MinValue);   // Validating json payload and return in error model format
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); // Force to save enum in string format to our database instead of magic numbers
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
    }

    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITodoService, TodoService>();
        services.AddScoped<IAuthService, AuthService>();
    }
}