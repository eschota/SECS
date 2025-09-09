using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace CoopBuilderServer.Services;

public record AssetCacheEntry(
    string asset_guid,
    string title,
    bool is_character,
    string[] keywords,
    int category_id,
    int sub_category_id,
    string preview_url,
    string glb_url,
    string metadata_url
);

public record SubCategoryCount(int sub_category_id, int total);
public record CategoryCount(int category_id, int total, List<SubCategoryCount> by_sub_category);

public class AssetIndexService
{
    private readonly ILogger<AssetIndexService> _logger;
    private readonly string _storageRoot;
    private readonly string _publicBaseUrl;
    private readonly int _formatVersion;
    private readonly bool _rebuildOnStart;
    private readonly List<AssetCacheEntry> _entries = new();
    private readonly object _lock = new();

    public AssetIndexService(IConfiguration config, ILogger<AssetIndexService> logger)
    {
        _logger = logger;
        var settings = config.GetSection("ServerSettings");
        _storageRoot = settings.GetValue<string>("AssetStorageRoot") ?? "c:/NDLWebServerBuild/wwwroot/glb_storage";
        _publicBaseUrl = settings.GetValue<string>("PublicBaseUrl") ?? "https://renderfin.com";
        _formatVersion = settings.GetValue<int>("AssetIndex:FormatVersion");
        _rebuildOnStart = settings.GetValue<bool>("AssetIndex:RebuildOnStart", true);
        Directory.CreateDirectory(_storageRoot);
        BuildIndex();
    }

    public IReadOnlyList<AssetCacheEntry> Entries
    {
        get { lock (_lock) return _entries.ToList(); }
    }

    public void BuildIndex()
    {
        lock (_lock)
        {
            _entries.Clear();
            foreach (var dir in Directory.EnumerateDirectories(_storageRoot))
            {
                var guid = Path.GetFileName(dir);
                if (string.IsNullOrWhiteSpace(guid)) continue;
                var jsonPath = Path.Combine(dir, $"{guid}.json");
                var pngPath = Path.Combine(dir, $"{guid}.png");
                var glbPath = Path.Combine(dir, $"{guid}.glb");
                if (!File.Exists(jsonPath) || !File.Exists(glbPath)) continue;

                try
                {
                    using var fs = File.OpenRead(jsonPath);
                    using var doc = JsonDocument.Parse(fs);
                    var root = doc.RootElement;
                    var title = root.TryGetProperty("title", out var t) ? t.GetString() ?? string.Empty : string.Empty;
                    var isCharacter = root.TryGetProperty("isCharacter", out var ic) && ic.GetBoolean();
                    var categoryId = root.TryGetProperty("categoryId", out var cid) ? cid.GetInt32() : 0;
                    var subCategoryId = root.TryGetProperty("subcategoryId", out var scid) ? scid.GetInt32() : 0;
                    var keywords = root.TryGetProperty("keywords", out var kw) && kw.ValueKind == JsonValueKind.Array
                        ? kw.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray()
                        : Array.Empty<string>();

                    var entry = new AssetCacheEntry(
                        asset_guid: guid,
                        title: title,
                        is_character: isCharacter,
                        keywords: keywords,
                        category_id: categoryId,
                        sub_category_id: subCategoryId,
                        preview_url: $"{_publicBaseUrl}/glb_storage/{guid}/{guid}.png",
                        glb_url: $"{_publicBaseUrl}/glb_storage/{guid}/{guid}.glb",
                        metadata_url: $"{_publicBaseUrl}/glb_storage/{guid}/{guid}.json"
                    );

                    _entries.Add(entry);

                    // Sidecar .bin
                    var binPath = Path.Combine(dir, $"{guid}.bin");
                    if (_rebuildOnStart || !File.Exists(binPath))
                    {
                        WriteSidecarBin(binPath, entry);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to index {guid}", guid);
                }
            }
        }
        _logger.LogInformation("Asset index built: {count} entries", _entries.Count);
    }

    private void WriteSidecarBin(string path, AssetCacheEntry entry)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        bw.Write(_formatVersion);

        var titleBytes = Encoding.UTF8.GetBytes(entry.title ?? string.Empty);
        bw.Write(titleBytes.Length);
        bw.Write(titleBytes);

        bw.Write(entry.is_character ? (byte)1 : (byte)0);
        bw.Write(entry.category_id);
        bw.Write(entry.sub_category_id);

        bw.Write(entry.keywords.Length);
        foreach (var kw in entry.keywords)
        {
            var kb = Encoding.UTF8.GetBytes(kw ?? string.Empty);
            bw.Write(kb.Length);
            bw.Write(kb);
        }
        bw.Flush();
        var raw = ms.ToArray();

        using var file = File.Create(path);
        using var deflate = new DeflateStream(file, CompressionLevel.Optimal);
        deflate.Write(raw, 0, raw.Length);
    }

    public (IEnumerable<AssetCacheEntry> items, int total) Search(string? q, int? categoryId, int? subCategoryId, bool? isCharacter, int limit, int offset)
    {
        var filtered = ApplyFilters(q, categoryId, subCategoryId, isCharacter);
        var total = filtered.Count();
        var items = filtered.Skip(Math.Max(0, offset)).Take(Math.Clamp(limit, 1, 200)).ToList();
        return (items, total);
    }

    public int Count(string? q, int? categoryId, int? subCategoryId, bool? isCharacter)
    {
        return ApplyFilters(q, categoryId, subCategoryId, isCharacter).Count();
    }

    public IReadOnlyList<CategoryCount> CategoryCounts(string? q, int? categoryId, int? subCategoryId, bool? isCharacter)
    {
        var filtered = ApplyFilters(q, categoryId, subCategoryId, isCharacter);
        var groups = filtered
            .GroupBy(e => e.category_id)
            .OrderBy(g => g.Key)
            .Select(g => new CategoryCount(
                category_id: g.Key,
                total: g.Count(),
                by_sub_category: g.GroupBy(e => e.sub_category_id)
                    .OrderBy(sg => sg.Key)
                    .Select(sg => new SubCategoryCount(sub_category_id: sg.Key, total: sg.Count()))
                    .ToList()
            ))
            .ToList();
        return groups;
    }

    private IEnumerable<AssetCacheEntry> ApplyFilters(string? q, int? categoryId, int? subCategoryId, bool? isCharacter)
    {
        q = (q ?? string.Empty).Trim();
        IEnumerable<AssetCacheEntry> query = Entries;
        if (!string.IsNullOrEmpty(q))
        {
            // Разбиваем запрос на токены (слова) и требуем совпадение всех токенов (AND)
            var tokens = q.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(t => t.ToLowerInvariant())
                          .ToArray();
            if (tokens.Length > 0)
            {
                query = query.Where(e =>
                {
                    var title = e.title ?? string.Empty;
                    // Токенизируем title и keywords в массивы слов (нижний регистр)
                    var titleWords = TokenizeToWords(title).ToArray();
                    var kwWords = e.keywords.SelectMany(k => TokenizeToWords(k ?? string.Empty)).ToArray();

                    bool ContainsToken(IEnumerable<string> words, string tok)
                    {
                        foreach (var w in words)
                        {
                            if (WordMatchesToken(w, tok)) return true;
                        }
                        return false;
                    }

                    return tokens.All(tok => ContainsToken(titleWords, tok) || ContainsToken(kwWords, tok));
                });
            }
        }
        if (categoryId.HasValue) query = query.Where(e => e.category_id == categoryId.Value);
        if (subCategoryId.HasValue) query = query.Where(e => e.sub_category_id == subCategoryId.Value);
        if (isCharacter.HasValue) query = query.Where(e => e.is_character == isCharacter.Value);
        return query;
    }

    private static IEnumerable<string> TokenizeToWords(string input)
    {
        if (string.IsNullOrEmpty(input)) yield break;
        var sb = new StringBuilder();
        foreach (var ch in input)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
            else
            {
                if (sb.Length > 0)
                {
                    yield return sb.ToString();
                    sb.Clear();
                }
            }
        }
        if (sb.Length > 0)
        {
            yield return sb.ToString();
        }
    }

    // Правило совпадения слова с токеном запроса:
    // 1) точное совпадение
    // 2) множественное число на 's' (cars -> car)
    // 3) числовой суффикс (car1 -> car)
    // Исключаем ложные вроде cartoon (cartoon != car, остаток не 's' и не цифры)
    private static bool WordMatchesToken(string wordLower, string tokenLower)
    {
        if (wordLower == tokenLower) return true;
        if (wordLower.Length > tokenLower.Length)
        {
            var rest = wordLower.AsSpan(tokenLower.Length);
            if (wordLower.StartsWith(tokenLower))
            {
                // Разрешаем только 's' или цифры в остатке
                if (rest.Length == 1 && rest[0] == 's') return true;
                bool allDigits = true;
                foreach (var ch in rest)
                {
                    if (!char.IsDigit(ch)) { allDigits = false; break; }
                }
                if (allDigits && rest.Length > 0) return true;
            }
        }
        return false;
    }
}


