using Microsoft.EntityFrameworkCore;
using Repcount.Data;
using Repcount.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<RepcountDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Repcount") ?? "Data Source=repcount.db"));
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<Clock>();
builder.Services.AddScoped<CurrentMember>();
builder.Services.AddScoped<TimetableService>();
builder.Services.AddScoped<UsageService>();
builder.Services.AddScoped<BookingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RepcountDbContext>();
    if (app.Configuration.GetValue("Repcount:ResetDatabaseOnStart", true))
    {
        await db.Database.EnsureDeletedAsync();
    }
    await db.Database.EnsureCreatedAsync();
    await SeedData.InitializeAsync(db, scope.ServiceProvider.GetRequiredService<Clock>());
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();

public partial class Program;
