using Middlewares.Exceptions;
using Microsoft.EntityFrameworkCore;
using Backend.Crypto;
using Backend.Security;
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

// Key ring for the compliant encryption endpoints: keys come from the environment (or the git-ignored dev key file).
builder.Services.AddSingleton<KeyRing>();
builder.Services.AddSingleton<IFieldCipher>(sp => sp.GetRequiredService<KeyRing>());

// TLS demo clients: "insecure-tls" skips certificate validation on purpose (vulnerable lab), "default-tls" keeps it.
builder.Services.AddHttpClient("insecure-tls")
  .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
  {
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
  });
builder.Services.AddHttpClient("default-tls");

var app = builder.Build();

// Apply pending migrations automatically
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider
      .GetRequiredService<AppDbContext>();

  await db.Database.MigrateAsync();
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
  app.UseSwagger();
  app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
  var serviceProvider = scope.ServiceProvider;
  var db = serviceProvider.GetService<AppDbContext>()
  ?? throw new InvalidOperationException("AppDbContext not found");

  SeedWeakHash.Seed(db);
  SeedSecureCredential.Seed(db);
  SeedResetToken.Seed(db);
  SeedLegacyCard.Seed(db);
  SeedProtectedCard.Seed(db, serviceProvider.GetRequiredService<IFieldCipher>());
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Secure endpoints: HTTPS only plus security headers (rejects plain HTTP with 400 before any redirect could leak a body).
app.UseMiddleware<SecurityHeadersMiddleware>();

// The vulnerable endpoints deliberately stay reachable over plain HTTP (encryption in transit not enforced).
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/lab/vulnerable"), branch => branch.UseHttpsRedirection());

app.UseAuthorization();

app.MapControllers();

app.MapSwagger();

app.Run();
