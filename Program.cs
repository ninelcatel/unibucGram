using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using unibucGram.Models; // eu pusesem appdbcontext.cs in models si de aia nu cred ca mergea, daca e cazu
// schimbam inapoi in .data 
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//as zice sa folosim direct User aici,oricum mosteneste IdentityUser
builder.Services.AddDefaultIdentity<User>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
})
.AddRoles<IdentityRole>() // Add this line if you intend to use roles
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddRazorPages();  // ADD THIS LINE
builder.Services.AddControllersWithViews();

var app = builder.Build();

// run EF Core migrations on startup with simple retry (ensures Docker DB is used/created)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    var attempts = 0;
    var maxAttempts = 10;
    while (true)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch
        {
            attempts++;
            if (attempts >= maxAttempts) throw;
            Thread.Sleep(5000);
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Only redirect to HTTPS when NOT running inside the container
var runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
if (!runningInContainer)
{
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Serve static files from wwwroot (uploads, css, js)
app.UseStaticFiles();

// Register static web assets (Razor class libraries)
app.MapStaticAssets();

app.MapRazorPages();  // This line should now work
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
