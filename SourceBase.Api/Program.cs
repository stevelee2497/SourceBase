using SourceBase.Api.Extensions;
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
builder.Services.AddValidation();
builder.Services.AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

if (app.Environment.IsProduction()) app.UseHttpsRedirection();

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<AuthorizationMiddleware>();
app.UseCors(Constants.CorsCustomPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.MapGroup("/api").RequireAuthorization().MapEndpoints(app);

app.Run();
