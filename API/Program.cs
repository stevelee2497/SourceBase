using API.Filters;
using API.Helpers;
using Core.DbContexts;
using Core.Entities;
using Core.Helpers;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Services.Todo;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add ASP.NET MVC services to the container.
builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ExceptionFilter>();
        options.Filters.Add<ModelValidationFilter>(int.MinValue);
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// Add EF Services
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer("name=ConnectionStrings:DefaultConnection"));
builder.Services.AddIdentityApiEndpoints<UserEntity>().AddEntityFrameworkStores<ApplicationDbContext>();

// Override Identity Authentication Configurations
builder.Services.AddOptions<BearerTokenOptions>(IdentityConstants.BearerScheme).Configure(options => 
{
    options.BearerTokenExpiration = TimeSpan.FromDays(30);
    options.RefreshTokenExpiration = TimeSpan.FromDays(60);
});

// Add application services
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISessionUserHelper, SessionUserHelper>();

var app = builder.Build();

// Configure the HTTP request middlewares pipeline.
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
