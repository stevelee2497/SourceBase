using Microsoft.AspNetCore.Components.Authorization;
using SourceBase.Web.Auth;
using SourceBase.Web.Services;

namespace SourceBase.Web;

public static class ProgramConfigurations
{
    public static void AddAppSettings(this IServiceCollection services, IConfiguration configuration)
    {
        var appSettings = configuration.Get<AppSettings>() ?? new AppSettings();
        services.AddSingleton(appSettings);
        services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(appSettings.ApiBaseUrl) });
    }

    public static void AddDependencyInjection(this IServiceCollection services)
    {
        services.AddScoped<BlazorAuthStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<BlazorAuthStateProvider>());
        services.AddScoped<ApiHttpClient>();
        services.AddScoped<ToastService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<UserTimeZoneService>();
    }
}
