using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Services;

public class MatchResult
{
    public int Score { get; set; }
    public List<string> MatchReasons { get; set; } = new();
}

public class ItemMatchResult
{
    public Item Item { get; set; } = null!;
    public int Score { get; set; }
    public List<string> MatchReasons { get; set; } = new();
}

public class ItemRequestMatchResult
{
    public ItemRequest ItemRequest { get; set; } = null!;
    public int Score { get; set; }
    public List<string> MatchReasons { get; set; } = new();
}

public class MatchingService
{
    private readonly AppDbContext _context;

    public MatchingService(AppDbContext context)
    {
        _context = context;
    }

    // ── Find ItemRequests that match a donated Item ───────────────────────────

    public async Task<List<ItemRequestMatchResult>> FindMatchesForItem(Item item)
    {
        var candidates = await _context.ItemRequests
            .Include(r => r.Images)
            .Include(r => r.User)
            .Where(r => r.Status == ItemRequestStatus.Open
                && r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        var results = new List<ItemRequestMatchResult>();

        foreach (var req in candidates)
        {
            var (score, reasons) = ScoreItemAgainstRequest(item, req);
            if (score >= 40)
            {
                results.Add(new ItemRequestMatchResult
                {
                    ItemRequest = req,
                    Score = score,
                    MatchReasons = reasons
                });
            }
        }

        return results.OrderByDescending(r => r.Score).Take(10).ToList();
    }

    // ── Find Items that match an ItemRequest ──────────────────────────────────

    public async Task<List<ItemMatchResult>> FindMatchesForItemRequest(ItemRequest itemRequest)
    {
        var query = _context.Items
            .Include(i => i.Images)
            .Include(i => i.User)
            .Where(i => i.Status == ItemStatus.Available
                && i.ExpiresAt > DateTime.UtcNow);

        if (itemRequest.KondisiMinimum == KondisiMinimum.Baru)
            query = query.Where(i => i.Kondisi == ItemCondition.Baru);

        var candidates = await query.ToListAsync();
        var results = new List<ItemMatchResult>();

        foreach (var item in candidates)
        {
            var (score, reasons) = ScoreRequestAgainstItem(itemRequest, item);
            if (score >= 40)
            {
                results.Add(new ItemMatchResult
                {
                    Item = item,
                    Score = score,
                    MatchReasons = reasons
                });
            }
        }

        return results.OrderByDescending(r => r.Score).Take(10).ToList();
    }

    // ── Scoring: Item → ItemRequest ───────────────────────────────────────────

    private (int score, List<string> reasons) ScoreItemAgainstRequest(Item item, ItemRequest req)
    {
        int score = 0;
        var reasons = new List<string>();

        if (item.Kategori == req.Kategori)
        {
            score += 40;
            reasons.Add("Kategori cocok");
        }
        else
        {
            return (0, reasons);
        }

        var keywordScore = ComputeKeywordScore(
            item.NamaBarang + " " + item.Deskripsi,
            req.Title + " " + req.Deskripsi);

        if (keywordScore > 0)
        {
            score += keywordScore;
            reasons.Add("Keyword cocok");
        }

        var locationResult = ScoreLocation(item.Lokasi, item.Provinsi, req.Lokasi, req.Provinsi);
        score += locationResult.points;
        if (locationResult.reason != null) reasons.Add(locationResult.reason);

        var detailScore = ScoreDetails(item.DetailTambahan, req.DetailTambahan, item.Kategori);
        score += detailScore.points;
        if (detailScore.reason != null) reasons.Add(detailScore.reason);

        return (score, reasons);
    }

    // ── Scoring: ItemRequest → Item ───────────────────────────────────────────

    private (int score, List<string> reasons) ScoreRequestAgainstItem(ItemRequest req, Item item)
    {
        int score = 0;
        var reasons = new List<string>();

        if (req.Kategori == item.Kategori)
        {
            score += 40;
            reasons.Add("Kategori cocok");
        }
        else
        {
            return (0, reasons);
        }

        var keywordScore = ComputeKeywordScore(
            req.Title + " " + req.Deskripsi,
            item.NamaBarang + " " + item.Deskripsi);

        if (keywordScore > 0)
        {
            score += keywordScore;
            reasons.Add("Keyword cocok");
        }

        var locationResult = ScoreLocation(req.Lokasi, req.Provinsi, item.Lokasi, item.Provinsi);
        score += locationResult.points;
        if (locationResult.reason != null) reasons.Add(locationResult.reason);

        var detailScore = ScoreDetails(req.DetailTambahan, item.DetailTambahan, req.Kategori);
        score += detailScore.points;
        if (detailScore.reason != null) reasons.Add(detailScore.reason);

        return (score, reasons);
    }

    // ── Keyword scoring ───────────────────────────────────────────────────────

    private int ComputeKeywordScore(string sourceText, string targetText)
    {
        if (string.IsNullOrWhiteSpace(sourceText) || string.IsNullOrWhiteSpace(targetText))
            return 0;

        var stopWords = new HashSet<string> { "dan", "atau", "yang", "di", "ke", "dari", "untuk", "dengan", "ini", "itu", "ada", "bisa", "mau", "saya", "anda", "a", "the", "of", "in", "for" };

        var sourceTokens = Tokenize(sourceText).Except(stopWords).ToHashSet();
        var targetTokens = Tokenize(targetText).Except(stopWords).ToHashSet();

        if (sourceTokens.Count == 0 || targetTokens.Count == 0) return 0;

        var overlap = sourceTokens.Intersect(targetTokens).Count();
        var ratio = (double)overlap / Math.Min(sourceTokens.Count, targetTokens.Count);

        return (int)Math.Round(ratio * 30);
    }

    private HashSet<string> Tokenize(string text)
    {
        return text.ToLower()
            .Split(new[] { ' ', ',', '.', '-', '/', '(', ')', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 3)
            .ToHashSet();
    }

    // ── Location scoring ──────────────────────────────────────────────────────

    private (int points, string? reason) ScoreLocation(string lokasi1, string provinsi1, string lokasi2, string provinsi2)
    {
        if (!string.IsNullOrWhiteSpace(lokasi1) && !string.IsNullOrWhiteSpace(lokasi2))
        {
            if (string.Equals(lokasi1.Trim(), lokasi2.Trim(), StringComparison.OrdinalIgnoreCase))
                return (20, "Kota sama");
        }

        if (!string.IsNullOrWhiteSpace(provinsi1) && !string.IsNullOrWhiteSpace(provinsi2))
        {
            if (string.Equals(provinsi1.Trim(), provinsi2.Trim(), StringComparison.OrdinalIgnoreCase))
                return (10, "Provinsi sama");
        }

        return (0, null);
    }

    // ── Detail scoring ────────────────────────────────────────────────────────

    private (int points, string? reason) ScoreDetails(string? details1Json, string? details2Json, ItemCategory category)
    {
        if (string.IsNullOrWhiteSpace(details1Json) || string.IsNullOrWhiteSpace(details2Json))
            return (0, null);

        try
        {
            var d1 = JsonSerializer.Deserialize<Dictionary<string, string>>(details1Json);
            var d2 = JsonSerializer.Deserialize<Dictionary<string, string>>(details2Json);

            if (d1 == null || d2 == null) return (0, null);

            var matchedFields = new List<string>();

            var keysToCompare = category switch
            {
                ItemCategory.Pakaian => new[] { "Ukuran", "JenisKelamin" },
                ItemCategory.Elektronik => new[] { "Merk", "Model" },
                ItemCategory.PerabotRumah => new[] { "Material" },
                ItemCategory.MainanHobi => new[] { "UsiaRekomendasi" },
                ItemCategory.AlatMusik => new[] { "JenisInstrumen", "Merk" },
                _ => Array.Empty<string>()
            };

            foreach (var key in keysToCompare)
            {
                if (d1.TryGetValue(key, out var v1) && d2.TryGetValue(key, out var v2))
                {
                    if (!string.IsNullOrWhiteSpace(v1) && !string.IsNullOrWhiteSpace(v2)
                        && string.Equals(v1.Trim(), v2.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        matchedFields.Add(key);
                    }
                }
            }

            if (matchedFields.Count == 0) return (0, null);

            var points = Math.Min(matchedFields.Count * 10, 20);
            return (points, $"Detail cocok: {string.Join(", ", matchedFields)}");
        }
        catch
        {
            return (0, null);
        }
    }
}