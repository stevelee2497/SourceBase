using SourceBase.Api;
using SourceBase.Api.Middlewares;
using SourceBase.Api.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.UseSeriLog();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwagger();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMvcConfigs();
builder.Services.AddAppSettings(builder.Configuration);
builder.Services.AddInfrastructure();
builder.Services.AddCorsPolicies(builder.Configuration);
builder.Services.AddFluentValidation(typeof(Program).Assembly);
builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.AddHandlers(typeof(Program).Assembly);

var app = builder.Build();

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
    app.EnsureDatabaseMigrated();
}

app.UseGlobalException();
app.UseCors(Constants.CorsCustomPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCustomAuthorization();
app.UseMinimalApi();

app.Run();
