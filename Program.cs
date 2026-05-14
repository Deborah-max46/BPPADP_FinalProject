using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using ConsumersVoiceSystemPrototype.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found.");

var usePostgres = connectionString.StartsWith("postgres", StringComparison.OrdinalIgnoreCase)
    || connectionString.StartsWith("Host=", StringComparison.OrdinalIgnoreCase);
var useSqlite = !usePostgres && (builder.Environment.IsDevelopment()
    || Environment.GetEnvironmentVariable("USE_SQLITE") == "1");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (usePostgres)
        options.UseNpgsql(connectionString);
    else if (useSqlite)
        options.UseSqlite(connectionString);
    else
        options.UseSqlServer(connectionString);
});

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddScoped<AppNotificationService>();
builder.Services.AddScoped<ComplaintWorkflowService>();
builder.Services.AddScoped<ComplaintAttachmentStorage>();
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (usePostgres || !useSqlite)
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();
}

await DbInitializer.SeedAsync(app.Services);

app.Run();
