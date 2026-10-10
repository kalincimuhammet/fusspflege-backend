using Fusspflege.Core.Interfaces;
using Fusspflege.Core.Services;
using Fusspflege.Infrastructure.Data;
using Fusspflege.Infrastructure.Email;
using Fusspflege.Infrastructure.Jobs;
using Fusspflege.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// CORS: nur die Adressen des Frontends dürfen die API aufrufen
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? Array.Empty<string>();
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigins)
        .WithMethods("GET", "POST")
        .WithHeaders("Content-Type")));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure()));

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
app.MapControllers();

app.Run();