using Serilog;
using SourceBase.EmailWorker;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((services, cfg) =>
    {
        cfg.ReadFrom.Configuration(builder.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext();

        var token = builder.Configuration["BetterStack:SourceToken"];
        if (!string.IsNullOrEmpty(token))
            cfg.WriteTo.BetterStack(token);
    });

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
