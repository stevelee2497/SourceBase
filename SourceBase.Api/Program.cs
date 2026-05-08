using SourceBase.Api.Extensions;
using SourceBase.Application;
using SourceBase.Application.Common;
using SourceBase.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.UseSeriLog();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwagger();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMvcConfigs();
builder.Services.AddAppSettings(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCorsPolicies(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
app.UseAuthorization();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();
app.UseSeeding();
app.UseCors(Constants.CorsCustomPolicy);

app.Run();
