using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SoftEng2025.Data;

var builder = WebApplication.CreateBuilder(args);

// 1) Configure EF Core to use PostgreSQL + PostGIS (via NetTopologySuite)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.UseNetTopologySuite()
    )
);

// 2) Add developer‐only DB exception page (for EF migrations errors in dev)
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 3) Configure Identity with Roles stored in your existing schema
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 4) Add Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

// 5) Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();    // optional: shows EF migration errors in dev
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 6) Enable authentication & authorization
app.UseAuthentication();
app.UseAuthorization();

// 7) Seed your roles on startup
using (var scope = app.Services.CreateScope())
{
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Critic", "Entrepreneur", "Admin" };
    foreach (var role in roles)
    {
        if (!await roleMgr.RoleExistsAsync(role))
        {
            await roleMgr.CreateAsync(new IdentityRole(role));
        }
    }
}

app.MapRazorPages();
app.Run();