using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CRMTranslationMerger.Server.Services;

public class OpenAiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiService> _logger;

    private readonly Dictionary<string, string> _placeholderMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "[Sender name]", "LoadedData.SenderProfile.Handle" },
        { "[Sender age]", "LoadedData.SenderProfile.Age" },
        { "[TimeAgo]", "LoadedData.TimeAgo" },
        { "[Time]", "LoadedData.Time" },
        { "[Recipient name]", "LoadedData.RecipientProfile.Handle" },
        { "[Recipient age]", "LoadedData.RecipientProfile.Age" }
    };

    public OpenAiService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Dictionary<string, string>> ConvertBatchAsync(Dictionary<string, string> texts)
    {
        var results = new Dictionary<string, string>();
        var successCount = 0;
        
        _logger.LogInformation("Starting batch conversion of {Count} texts", texts.Count);
        
        foreach (var (key, text) in texts)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    results[key] = text;
                    continue;
                }

                var converted = ConvertPlaceholdersToHandlebars(text);
                results[key] = converted;
                
                if (converted != text)
                {
                    successCount++;
                    _logger.LogDebug("Converted {Key}: {Original} → {Converted}", key, text, converted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting key: {Key}", key);
                results[key] = text;
            }
        }

        _logger.LogInformation("Batch conversion complete: {Success} converted out of {Total}", 
            successCount, texts.Count);

        return results;
    }

    private string ConvertPlaceholdersToHandlebars(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // BEST EFFORT REPAIR: Fix common malformed placeholder issues
        text = RepairMalformedPlaceholders(text);

        var placeholderPattern = @"\[([^\]]+)\]";
        var matches = Regex.Matches(text, placeholderPattern);

        if (matches.Count == 0)
            return text;

        if (matches.Count == 1 && matches[0].Value == text)
        {
            var placeholder = matches[0].Value;
            if (_placeholderMappings.TryGetValue(placeholder, out var handlebarsVar))
            {
                return $"{{{{{handlebarsVar}}}}}";
            }
            
            // Unknown placeholder
            _logger.LogWarning("Unknown placeholder: {Placeholder} in text: {Text}", placeholder, text);
            return text;
        }

        return ConvertComplexText(text, matches);
    }

    /// <summary>
    /// Best effort repair for common malformed placeholder patterns
    /// </summary>
    private string RepairMalformedPlaceholders(string text)
    {
        var original = text;
        
        // Pattern 1: [Placeholder1 some text [Placeholder2] - missing ] after Placeholder1
        // Fix by adding ] after known placeholder names when followed by lowercase text
        foreach (var (placeholder, _) in _placeholderMappings)
        {
            var placeholderName = placeholder.TrimStart('[').TrimEnd(']');
            
            // Match: [PlaceholderName followed by space and lowercase letter (not another placeholder)
            var pattern = $@"\[{Regex.Escape(placeholderName)}(\s+[a-zà-ÿ])";
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            
            if (match.Success)
            {
                // Insert ] after the placeholder name
                text = Regex.Replace(text, pattern, $"[{placeholderName}]$1", RegexOptions.IgnoreCase);
                _logger.LogInformation("🔧 Repaired malformed placeholder: Added ']' after [{PlaceholderName}]", placeholderName);
            }
        }

        // Pattern 2: Nested brackets like [[Placeholder]] -> [Placeholder]
        if (text.Contains("[["))
        {
            text = text.Replace("[[", "[").Replace("]]", "]");
            _logger.LogInformation("🔧 Repaired double brackets in text");
        }

        if (text != original)
        {
            _logger.LogInformation("📝 Repaired text: {Original} → {Repaired}", original, text);
        }

        return text;
    }

    private string ConvertComplexText(string text, MatchCollection matches)
    {
        var parts = new List<string>();
        var lastIndex = 0;

        foreach (Match match in matches)
        {
            var placeholder = match.Value;
            var placeholderIndex = match.Index;

            if (placeholderIndex > lastIndex)
            {
                var beforeText = text.Substring(lastIndex, placeholderIndex - lastIndex);
                parts.Add($"\"{EscapeQuotes(beforeText)}\"");
            }

            if (_placeholderMappings.TryGetValue(placeholder, out var handlebarsVar))
            {
                parts.Add(handlebarsVar);
            }
            else
            {
                parts.Add($"\"{EscapeQuotes(placeholder)}\"");
            }

            lastIndex = placeholderIndex + placeholder.Length;
        }

        if (lastIndex < text.Length)
        {
            var afterText = text.Substring(lastIndex);
            parts.Add($"\"{EscapeQuotes(afterText)}\"");
        }

        if (parts.Count == 0)
            return text;
        
        if (parts.Count == 1)
        {
            return parts[0].StartsWith("\"") ? text : $"{{{{{parts[0]}}}}}";
        }

        var concatenated = string.Join(" ", parts);
        return $"{{{{String.Concat {concatenated}}}}}";
    }

    private string EscapeQuotes(string text)
    {
        return text.Replace("\"", "\\\"");
    }

    public async Task<(bool Success, string? ConvertedText, string? Error)> ConvertPlaceholderAsync(
        string text, 
        object? jsonContext = null, 
        object? excelContext = null)
    {
        try
        {
            var converted = ConvertPlaceholdersToHandlebars(text);
            return (true, converted, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting placeholder");
            return (false, null, ex.Message);
        }
    }
}
