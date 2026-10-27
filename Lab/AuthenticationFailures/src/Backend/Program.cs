using backend.Security;
using backend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
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


builder.Services.UseRateLimit();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ExceptionHandlingMiddleware>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<SecurityEventService>();
builder.Services.AddScoped<TokenService>();

// Authentication: bearer JWT + server side revocation checks.
builder.Services.AddAuthentication(LabAuth.Scheme)
  .AddScheme<AuthenticationSchemeOptions, RevocableJwtAuthenticationHandler>(LabAuth.Scheme, _ => { });

// Authorization is deny by default: any endpoint without [AllowAnonymous] needs an authenticated user.
builder.Services.AddAuthorization(options =>
{
  options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();

  options.AddPolicy(LabAuth.AdminPolicy, policy => policy.RequireRole(Models.Entity.Roles.Admin));
});
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AccessControlResultHandler>();

// SECURE: CORS is off unless origins are listed explicitly in configuration; never "any origin".
string[] corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
  if (corsOrigins.Length > 0)
  {
    options.AddDefaultPolicy(policy => policy
      .WithOrigins(corsOrigins)
      .WithMethods("GET", "POST", "PUT", "DELETE")
      .WithHeaders("Authorization", "Content-Type"));
  }

  // VULNERABLE (used only by lab/vulnerable/* via [EnableCors("Vulnerable")]): reflects any origin and allows
  // credentials, so any web site can call the API with the victim's cookies/tokens.
  options.AddPolicy("Vulnerable", policy => policy
    .SetIsOriginAllowed(_ => true)
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());
});

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
  SeedUser.Seed(db);
  SeedUserRole.Seed(db);
  SeedNote.Seed(db);
  SeedLegacyNote.Seed(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.MapOpenApi().AllowAnonymous();
  app.UseSwagger();
  app.UseSwaggerUI();
  app.MapSwagger().AllowAnonymous();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseRateLimiter();

app.UseMiddleware<HardeningMiddleware>();

app.UseHttpsRedirection();

app.UseCors();

// VULNERABLE: static file server with directory listing over a folder that contains repository metadata and
// backup files (rule 5). The secure endpoints serve no static files at all.
string vulnerablePublicRoot = Path.Combine(AppContext.BaseDirectory, "vuln-public");
VulnerableStaticFiles.Prepare(vulnerablePublicRoot);
app.UseFileServer(new FileServerOptions
{
  FileProvider = new PhysicalFileProvider(vulnerablePublicRoot, ExclusionFilters.None),
  RequestPath = VulnerableStaticFiles.RequestPath,
  EnableDirectoryBrowsing = true,
  StaticFileOptions = { ServeUnknownFileTypes = true },
});

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
