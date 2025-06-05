using Microsoft.EntityFrameworkCore;
using ContosoUniversityCore.Data;
using Microsoft.Data.Sqlite; // Added for SqliteConnection

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Setup a single, open SqliteConnection for in-memory database to be shared.
var connectionString = builder.Configuration.GetConnectionString("SchoolContext") ?? "DataSource=InMemoryShared;Mode=Memory;Cache=Shared";
var keepAliveConnection = new SqliteConnection(connectionString);
keepAliveConnection.Open(); // Keep the connection open for the lifetime of the app

builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlite(keepAliveConnection)); // Use the shared, open connection

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SchoolContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
