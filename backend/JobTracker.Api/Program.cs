using System.Text;
using JobTracker.Api.Data;
using JobTracker.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- Database ---
// SQLite for simplicity: no separate database server to install or run.
// Swap the provider (e.g. Npgsql for Postgres) here for production.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=jobtracker.db"));

// --- Dependency injection ---
// Registering interfaces -> implementations lets controllers depend on
// IJobApplicationService / ITokenService rather than concrete classes,
// which is what makes those controllers (and the services themselves)
// easy to unit test in isolation.
builder.Services.AddScoped<IJobApplicationService, JobApplicationService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// --- JWT authentication ---
var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-only-change-me-in-production-please";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

builder.Services.AddAuthorization();

// --- CORS ---
// The React dev server runs on a different port than the API, so the
// browser treats it as a different origin and blocks requests unless
// we explicitly allow it.
const string CorsPolicy = "FrontendPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Create the SQLite database and tables directly from the model on
// startup. This avoids needing the separate `dotnet-ef` tool and a
// migrations folder for a project this size — the tradeoff is that it
// can't evolve an existing schema over time. A production app with a
// real user base would use versioned EF Core migrations (`dotnet ef
// migrations add ...`) instead, precisely so the schema can change
// without losing existing data.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposes the implicit Program class so the test project can reference
// it (needed for WebApplicationFactory-style integration testing).
public partial class Program { }
