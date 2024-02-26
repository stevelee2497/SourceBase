using Core.DbContexts;
using Microsoft.EntityFrameworkCore;
using Services;

var builder = WebApplication.CreateBuilder(args);

// Add ASP.NET MVC services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

// Add EF Services
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer("Data Source=localhost;Initial Catalog=SourceBase;TrustServerCertificate=True;User ID=sa;Password=Root@123"));

// Add application services
builder.Services.AddScoped<ITodoService, TodoService>();

var app = builder.Build();

// Configure the HTTP request middlewares pipeline.
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
