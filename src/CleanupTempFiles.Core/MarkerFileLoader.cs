using System.Text.Json;

namespace CleanupTempFiles;

public static class MarkerFileLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static MarkerFile? TryLoad(string directoryPath, string markerFileName, out string? error)
    {
        error = null;
        var markerPath = Path.Combine(directoryPath, markerFileName);
        if (!File.Exists(markerPath))
            return null;

        try
        {
            var json = File.ReadAllText(markerPath);
            var dto = JsonSerializer.Deserialize<MarkerFileDto>(json, JsonOptions);
            if (dto is null || dto.Rules.Count == 0)
            {
                error = "Marker-Datei enthält keine Regeln.";
                return null;
            }

            var rules = dto.Rules
                .Select(r => new CleanupRule(r.Pattern, TimeSpan.Parse(r.OlderThan)))
                .ToList();

            return new MarkerFile(dto.Recursive, rules);
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            error = $"Marker-Datei ist ungültig: {ex.Message}";
            return null;
        }
    }

    private sealed record MarkerFileDto(bool Recursive, List<MarkerRuleDto> Rules);

    private sealed record MarkerRuleDto(string Pattern, string OlderThan);
}
