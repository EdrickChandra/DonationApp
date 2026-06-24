using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Services;

public static class ImageStorage
{
    public static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
    public const long MaxBytes = 5 * 1024 * 1024;

    public static bool IsValidImage(IFormFile image)
    {
        var ext = Path.GetExtension(image.FileName).ToLower();
        return AllowedExtensions.Contains(ext) && image.Length <= MaxBytes;
    }

    public static async Task SaveImagesAsync(
        AppDbContext db,
        string webRootPath,
        IEnumerable<IFormFile> images,
        int maxCount,
        int currentCount,
        Action<ItemImage> configureOwner,
        params string[] folderSegments)
    {
        var uploadFolder = Path.Combine(new[] { webRootPath }.Concat(folderSegments).ToArray());
        Directory.CreateDirectory(uploadFolder);
        var urlBase = "/" + string.Join("/", folderSegments);

        int count = currentCount;
        foreach (var image in images)
        {
            if (count >= maxCount) break;
            if (!IsValidImage(image)) continue;

            var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName).ToLower();
            using (var stream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
                await image.CopyToAsync(stream);

            var img = new ItemImage
            {
                FileSize = image.Length,
                FilePath = $"{urlBase}/{fileName}"
            };
            configureOwner(img);
            db.ItemImages.Add(img);
            count++;
        }
    }

    public static void DeleteFileIfExists(string webRootPath, string relativePath)
    {
        var fullPath = Path.Combine(webRootPath, relativePath.TrimStart('/'));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
