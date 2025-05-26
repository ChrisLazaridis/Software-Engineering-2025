using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SoftEng2025.Data;
using Microsoft.Extensions.DependencyInjection;
using SoftEng2025.Services;

var builder = WebApplication.CreateBuilder(args);

// 1) Configure EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.UseNetTopologySuite()
    )
);

// 2) Add Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        // (other IdentityOptions you may have…)
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 3) Force V3 hasher + 100k iterations
builder.Services.Configure<PasswordHasherOptions>(opts =>
{
    // Use the ASP.NET Core 3+/V3 format blob
    opts.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
    // Match .NET 7’s default 100 000 rounds (override if you’ve changed it)
    opts.IterationCount = 100_000;
});
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
});

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});
// 4) Razor Pages, middleware, role‐seeding, etc.
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache(); // ← for caching geocoding results
builder.Services
    .AddHttpClient<IGeocodingService, GeocodingService>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));
builder.Services.AddRazorPages();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();              // ← Serves CSS/JS/images from wwwroot
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Seed roles on startup
using (var scope = app.Services.CreateScope())
{
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Critic", "Entrepreneur", "Admin" })
        if (!await roleMgr.RoleExistsAsync(role))
            await roleMgr.CreateAsync(new IdentityRole(role));
}

app.MapRazorPages();
app.Run();