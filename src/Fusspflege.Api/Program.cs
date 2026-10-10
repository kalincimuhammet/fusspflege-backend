using System.Security.Claims;
using System.Text;
using Fusspflege.Api.Auth;
using Fusspflege.Core.Interfaces;
using Fusspflege.Core.Services;
using Fusspflege.Infrastructure.Data;
using Fusspflege.Infrastructure.Email;
using Fusspflege.Infrastructure.Jobs;
using Fusspflege.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// CORS: nur die Adressen des Frontends dürfen die API aufrufen
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? Array.Empty<string>();
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigins)
        .WithMethods("GET", "POST", "PUT", "DELETE")
        .WithHeaders("Content-Type", "Authorization")));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure()));

// Identity: Benutzer, Passwort-Hashes und Rollen in der Datenbank
builder.Services
    .AddIdentityCore<IdentityUser>(options =>
    {
        options.Password.RequiredLength = 12;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// JWT: Der Schlüssel muss gesetzt sein, sonst startet die API bewusst nicht
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    throw new InvalidOperationException(
        "Jwt:Key fehlt oder ist zu kurz (mindestens 32 Zeichen). Bitte per User Secrets bzw. App Settings setzen.");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<JwtTokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton<SlotCalculator>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<AppointmentService>();

// E-Mail: Provider per Konfiguration wählbar (Console oder Acs)
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
if (string.Equals(builder.Configuration["Email:Provider"], "Acs", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IEmailSender, AcsEmailSender>();
else
    builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();
builder.Services.AddScoped<BookingEmailService>();

// Erinnerungen: Logik als Service, Timer als Hintergrunddienst
builder.Services.AddScoped<ReminderService>();
builder.Services.AddHostedService<ReminderBackgroundService>();

// Ab hier sind keine Services mehr hinzufügbar
var app = builder.Build();

// Rolle und ersten Admin anlegen (nur wenn Admin:Email und Admin:Password gesetzt sind)
using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAdminAsync(
        scope.ServiceProvider,
        app.Configuration["Admin:Email"],
        app.Configuration["Admin:Password"]);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Fusspflege API"));

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

// In der Entwicklung ohne Umleitung auf HTTPS, sonst scheitern Browser-Anfragen von Angular an der Weiterleitung
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();