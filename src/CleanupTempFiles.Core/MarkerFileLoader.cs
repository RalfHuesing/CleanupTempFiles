using System.Globalization;
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
            if (dto is null || dto.Rules is not { Count: > 0 })
            {
                error = "Marker-Datei enthält keine Regeln.";
                return null;
            }

            var rules = new List<CleanupRule>(dto.Rules.Count);
            foreach (var rule in dto.Rules)
            {
                if (string.IsNullOrWhiteSpace(rule.Pattern))
                {
                    error = "Marker-Datei enthält eine Regel ohne 'pattern'.";
                    return null;
                }

                if (string.IsNullOrWhiteSpace(rule.OlderThan))
                {
                    error = $"Regel '{rule.Pattern}' hat kein 'olderThan'.";
                    return null;
                }

                if (!TimeSpan.TryParse(rule.OlderThan, CultureInfo.InvariantCulture, out var olderThan) || olderThan < TimeSpan.Zero)
                {
                    error = $"Regel '{rule.Pattern}' hat ein ungültiges 'olderThan': '{rule.OlderThan}'.";
                    return null;
                }

                rules.Add(new CleanupRule(rule.Pattern, olderThan));
            }

            return new MarkerFile(dto.Recursive, rules);
        }
        catch (JsonException ex)
        {
            error = $"Marker-Datei ist ungültig: {ex.Message}";
            return null;
        }
    }

    private sealed record MarkerFileDto(bool Recursive, List<MarkerRuleDto>? Rules);

    private sealed record MarkerRuleDto(string? Pattern, string? OlderThan);
}
