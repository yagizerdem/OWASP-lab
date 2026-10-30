using backend.Services;
using Microsoft.EntityFrameworkCore;
using Middlewares.Exceptions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapip

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenApi("OWASP-lab-API", options =>
{
  options.ShouldInclude = (description) =>
    {
      return description.RelativePath != null && description.RelativePath.StartsWith("lab/");
    };
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ExceptionHandlingMiddleware>();

// Failed login counters (example 2) and the logout deny-list (example 3) live in memory.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<TokenFactory>();

var app = builder.Build();

// Apply pending migrations automatically
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider
      .GetRequiredService<AppDbContext>();

  await db.Database.MigrateAsync();
  SeedUser.Seed(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
