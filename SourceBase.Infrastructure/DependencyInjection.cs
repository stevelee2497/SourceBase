using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SourceBase.Application.Abstractions;
using SourceBase.Infrastructure.DbContexts;
using SourceBase.Infrastructure.Helpers;
using SourceBase.Infrastructure.Identity;

namespace SourceBase.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<ApplicationDbContext>();

        // Identity
        services.AddIdentityApiEndpoints<ApplicationUser>()
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        // Application abstractions
        services.AddScoped<IDbContext, ApplicationDbContext>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<IEmailHelper, SendGridEmailHelper>();

        return services;
    }
}
