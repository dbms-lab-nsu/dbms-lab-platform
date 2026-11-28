using System.Text;
using System.Text.Json.Nodes;
using DBMSLab.Api.Application.Content;
using DBMSLab.Api.Application.Content.Contracts;
using DBMSLab.Api.Auth;
using DBMSLab.Api.Contracts;
using DBMSLab.Api.Data;
using DBMSLab.Api.Domain;
using DBMSLab.Api.Repositories;
using DBMSLab.Api.Repositories.Persistence;
using DBMSLab.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? builder.Configuration["DATABASE_CONNECTION_STRING"]
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Default or DATABASE_CONNECTION_STRING.");

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Configure Jwt:Key (environment variable Jwt__Key).");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must contain at least 32 UTF-8 bytes.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "dbms-lab";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "dbms-lab-client";

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<IProgressRepository, ProgressRepository>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IProgressService, ProgressService>();

builder.Services.AddScoped<LearningAccessService>();
builder.Services.AddScoped<DemoDataSeeder>();

builder.Services.AddScoped<ITestRepository, TestRepository>();
builder.Services.AddScoped<ITestAttemptRepository, TestAttemptRepository>();
builder.Services.AddScoped<ITestService, TestService>();

builder.Services.AddOpenApi(options =>
    options.AddSchemaTransformer((schema, context, _) =>
    {
        if (context.JsonTypeInfo.Type == typeof(UpdateProfileRequest) ||
            context.JsonTypeInfo.Type == typeof(UpdateCourseRequest))
        {
            schema.MinProperties = 1;

            foreach (var field in
                schema.Properties?.Values.OfType<OpenApiSchema>() ?? [])
            {
                field.Type = JsonSchemaType.String;
            }
        }

        if (context.JsonTypeInfo.Type == typeof(LessonProgressRequest)
            || context.JsonTypeInfo.Type == typeof(LessonProgressResponse))
        {
            // Describe the strict string converter; nullable storage is used only for Required validation.
            foreach (var key in schema.Properties?.Keys
                .Where(key => key.Equals("status", StringComparison.OrdinalIgnoreCase)).ToList() ?? [])
            {
                schema.Properties![key] = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Enum = Enum.GetNames<LessonProgressStatus>()
                        .Select(name => (JsonNode)JsonValue.Create(name)!).ToList()
                };
            }
        }

        return Task.CompletedTask;
    }));

builder.Services.AddCors(options =>
    options.AddPolicy("LocalFrontend", policy => policy
        .WithOrigins("http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors("LocalFrontend");

// Demo data seeding (Development only).
// Runs after PostgreSQL migrations have been applied, either when
// SEED_DEMO_DATA=true or when the --seed argument is passed.
// Does not create test users with fixed passwords — the demo author has
// no password and is not intended for sign-in.
if (app.Environment.IsDevelopment())
{
    bool isSeedCommand = args.Length > 0 && args[0] == "--seed";
    bool isSeedEnv = bool.TryParse(builder.Configuration["SEED_DEMO_DATA"], out var seedDemo) && seedDemo;

    if (isSeedCommand || isSeedEnv)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await seeder.SeedAsync();

        // When invoked via --seed, exit immediately without starting the HTTP server.
        if (isSeedCommand)
            return;
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "OK", service = "DBMS Lab Backend" }));
app.MapGet("/api/health/db", async (AppDbContext db) =>
    await db.Database.CanConnectAsync()
        ? Results.Ok(new { status = "OK", database = "PostgreSQL" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

app.MapControllers();
app.Run();
