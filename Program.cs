using DonationApp.Data;
using DonationApp.Models;
using DonationApp.Hubs;
using DonationApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();



builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
});

builder.Services.AddSignalR();
builder.Services.AddScoped<MatchingService>();
builder.Services.AddScoped<PointsService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.RedeemItems.Any())
    {
        db.RedeemItems.AddRange(
            new RedeemItem { Name = "Voucher Belanja Rp10.000", Description = "Voucher belanja online senilai Rp10.000", PointCost = 50, Stock = 100, IsActive = true },
            new RedeemItem { Name = "Voucher Belanja Rp25.000", Description = "Voucher belanja online senilai Rp25.000", PointCost = 120, Stock = 50, IsActive = true },
            new RedeemItem { Name = "Stiker IDonasi Eksklusif", Description = "Paket stiker eksklusif dari IDonasi", PointCost = 30, Stock = 200, IsActive = true },
            new RedeemItem { Name = "Pin IDonasi", Description = "Pin eksklusif IDonasi untuk koleksi", PointCost = 20, Stock = 150, IsActive = true }
        );
        db.SaveChanges();
    }
}

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var adminEmail = "admin@idonasi.com";
    var existing = await userManager.FindByEmailAsync(adminEmail);

    if (existing == null)
    {
        var admin = new ApplicationUser
        {
            UserName = "Admin",
            Email = adminEmail,
            NamaDepan = "Admin",
            NamaBelakang = "IDonasi",
            Alamat = "Jakarta",
            Kota = "Jakarta Pusat",
            Provinsi = "DKI Jakarta",
            IsAdmin = true,
            CreatedAt = DateTime.UtcNow
        };

        await userManager.CreateAsync(admin, "Admin123!");
    }
    else if (!existing.IsAdmin)
    {
        existing.IsAdmin = true;
        await userManager.UpdateAsync(existing);
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.File.Name;
        if (path.EndsWith(".css") || path.EndsWith(".js") || path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".woff2"))
        {
            ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=604800";
        }
        else
        {
            ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store";
            ctx.Context.Response.Headers["Pragma"] = "no-cache";
            ctx.Context.Response.Headers["Expires"] = "0";
        }
    }
});
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ChatHub>("/chatHub");

app.Run();