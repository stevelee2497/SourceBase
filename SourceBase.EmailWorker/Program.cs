using Serilog;
using SourceBase.Application.Shared;
using SourceBase.EmailWorker;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((services, cfg) => cfg
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    var appSettings = builder.Configuration.Get<AppSettings>()!;
    builder.Services.AddSingleton(appSettings);
    builder.Services.AddHostedService<EmailConsumerService>();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Email worker terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
