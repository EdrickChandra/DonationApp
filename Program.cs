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
    db.Database.Migrate();
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    if (!db.Items.Any())
    {
        async Task<ApplicationUser> EnsureDonor(string email, string depan, string belakang, string kota, string provinsi, string alamat)
        {
            var u = await userManager.FindByEmailAsync(email);
            if (u == null)
            {
                u = new ApplicationUser
                {
                    UserName = depan,
                    Email = email,
                    NamaDepan = depan,
                    NamaBelakang = belakang,
                    Alamat = alamat,
                    Kota = kota,
                    Provinsi = provinsi,
                    CreatedAt = DateTime.UtcNow
                };
                await userManager.CreateAsync(u, "Demo123!");
            }
            return u;
        }

        var budi = await EnsureDonor("budi@idonasi.com", "Budi", "Santoso", "Bandung", "Jawa Barat", "Jl. Merdeka No. 1");
        var siti = await EnsureDonor("siti@idonasi.com", "Siti", "Rahma", "Jakarta Selatan", "DKI Jakarta", "Jl. Sudirman No. 2");
        var seedAdmin = await userManager.FindByEmailAsync("admin@idonasi.com");

        Item Donation(string nama, ItemCondition kondisi, ItemCategory kat, int jumlah, string lokasi, string provinsi, string deskripsi, ApplicationUser owner, string img, string? detail = null)
            => new Item
            {
                NamaBarang = nama,
                Kondisi = kondisi,
                Kategori = kat,
                Status = ItemStatus.Available,
                Jumlah = jumlah,
                Lokasi = lokasi,
                Provinsi = provinsi,
                Deskripsi = deskripsi,
                DetailTambahan = detail,
                UserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                Images = new List<ItemImage> { new ItemImage { OwnerType = ImageOwnerType.Donation, FilePath = $"/uploads/seed/{img}" } }
            };

        db.Items.AddRange(
            Donation("Jaket Hoodie Abu-abu", ItemCondition.Bekas, ItemCategory.Pakaian, 1, "Bandung", "Jawa Barat", "Hoodie ukuran L, bahan tebal, masih sangat layak pakai.", budi, "jaket-hoodie.jpg"),
            Donation("Sepatu Sneakers Putih", ItemCondition.Bekas, ItemCategory.Pakaian, 1, "Bandung", "Jawa Barat", "Sneakers ukuran 42, sol masih bagus, cocok sehari-hari.", budi, "sepatu-sneakers.jpg"),
            Donation("Laptop Asus VivoBook", ItemCondition.Bekas, ItemCategory.Elektronik, 1, "Jakarta Selatan", "DKI Jakarta", "Laptop Core i3, RAM 4GB, masih normal untuk tugas ringan.", siti, "laptop-asus.png", "{\"Jenis\":\"Laptop\",\"Merk\":\"Asus\"}"),
            Donation("Kipas Angin Berdiri", ItemCondition.Bekas, ItemCategory.Elektronik, 1, "Jakarta Selatan", "DKI Jakarta", "Kipas angin berdiri 3 kecepatan, berfungsi dengan baik.", siti, "kipas-angin.jpg"),
            Donation("Meja Belajar Kayu Jati", ItemCondition.Bekas, ItemCategory.PerabotRumah, 1, "Jakarta Pusat", "DKI Jakarta", "Meja belajar kayu jati kokoh, ada laci penyimpanan.", seedAdmin!, "meja-belajar.jpg"),
            Donation("Set Lego Classic", ItemCondition.Baru, ItemCategory.Mainan, 2, "Jakarta Selatan", "DKI Jakarta", "Set Lego Classic baru, lengkap dalam kotak, belum dibuka.", siti, "lego-classic.jpg"),
            Donation("Boneka Teddy Bear Besar", ItemCondition.Bekas, ItemCategory.Mainan, 1, "Bandung", "Jawa Barat", "Boneka teddy bear ukuran besar, bersih dan masih empuk.", budi, "boneka-teddy.jpg"),
            Donation("Gitar Akustik Yamaha F310", ItemCondition.Bekas, ItemCategory.AlatMusik, 1, "Jakarta Pusat", "DKI Jakarta", "Gitar akustik Yamaha F310, senar baru, suara masih jernih.", seedAdmin!, "gitar-yamaha.jpg"),
            Donation("Seragam Sekolah SD", ItemCondition.Bekas, ItemCategory.Pakaian, 1, "Bandung", "Jawa Barat", "Seragam sekolah SD lengkap, ukuran anak 8 tahun, masih bagus.", siti, "seragam-sd.jpg", "{\"Ukuran\":\"8 tahun\",\"JenisKelamin\":\"Laki-laki\"}")
        );
        await db.SaveChangesAsync();

        ItemRequest WantedItem(string title, ItemCategory kat, string lokasi, string provinsi, string deskripsi, ApplicationUser owner, string img, string? detail = null)
            => new ItemRequest
            {
                Title = title,
                Kategori = kat,
                Status = ItemRequestStatus.Open,
                Jumlah = 1,
                Lokasi = lokasi,
                Provinsi = provinsi,
                Deskripsi = deskripsi,
                DetailTambahan = detail,
                UserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(14),
                Images = new List<ItemImage> { new ItemImage { OwnerType = ImageOwnerType.Request, FilePath = $"/uploads/seed/{img}" } }
            };

        db.ItemRequests.AddRange(
            WantedItem("Mencari Seragam Sekolah SD", ItemCategory.Pakaian, "Bandung", "Jawa Barat", "Membutuhkan seragam sekolah SD ukuran anak 8 tahun untuk keluarga.", budi, "req-seragam.jpg", "{\"Ukuran\":\"8 tahun\",\"JenisKelamin\":\"Laki-laki\"}"),
            WantedItem("Butuh Laptop untuk Belajar Online", ItemCategory.Elektronik, "Jakarta Selatan", "DKI Jakarta", "Mencari laptop bekas layak pakai untuk anak belajar online.", budi, "req-laptop.jpg", "{\"Jenis\":\"Laptop\",\"Merk\":\"Asus\"}")
        );
        await db.SaveChangesAsync();
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store";
        ctx.Context.Response.Headers["Pragma"] = "no-cache";
        ctx.Context.Response.Headers["Expires"] = "0";
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