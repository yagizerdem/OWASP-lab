using Backend.Crypto;
using Middlewares.Exceptions;
using Microsoft.EntityFrameworkCore;
using Models.Crypto;

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

// Card number encryption for the secure endpoints; the key comes from the environment or a git-ignored dev file.
builder.Services.AddSingleton<ICardCipher, CardCipher>();

var app = builder.Build();

// Apply pending migrations automatically
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider
      .GetRequiredService<AppDbContext>();

  await db.Database.MigrateAsync();
}

using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
  SeedWeakHash.Seed(db);
  SeedSecureCredential.Seed(db);
  SeedLegacyCard.Seed(db);
  SeedProtectedCard.Seed(db, scope.ServiceProvider.GetRequiredService<ICardCipher>());
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

app.UseAuthorization();

app.MapControllers();

app.MapSwagger();

app.Run();
