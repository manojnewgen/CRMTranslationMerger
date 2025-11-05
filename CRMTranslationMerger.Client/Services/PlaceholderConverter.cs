using System.Text.RegularExpressions;

namespace CRMTranslationMerger.Client.Services;

public class PlaceholderConverter
{
    private readonly Dictionary<string, string> _placeholderMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "[Sender name]", "LoadedData.SenderProfile.Handle" },
        { "[Sender age]", "LoadedData.SenderProfile.Age" },
        { "[TimeAgo]", "LoadedData.TimeAgo" },
        { "[Time]", "LoadedData.Time" },
        { "[Recipient name]", "LoadedData.RecipientProfile.Handle" },
        { "[Recipient age]", "LoadedData.RecipientProfile.Age" }
    };

    // Store detected patterns per translation key (e.g., "subject" -> "String.Append")
    private readonly Dictionary<string, string> _detectedPatterns = new();

    public Dictionary<string, string> ConvertBatch(Dictionary<string, string> texts)
    {
        var results = new Dictionary<string, string>();
        var successCount = 0;
        
        Console.WriteLine($"Starting batch conversion of {texts.Count} texts");
        
        // PHASE 1: Analyze reference language (usually 'en') to detect patterns
        var referenceLang = "en";
        var referenceKeys = texts.Keys.Where(k => k.StartsWith($"{referenceLang}|")).ToList();
        
        foreach (var key in referenceKeys)
        {
            var text = texts[key];
            var translationKey = key.Split('|')[1]; // Extract "subject", "messageReceived", etc.
            
            // Detect if it already uses String.Append or String.Concat
            var detectedPattern = DetectHandlebarsPattern(text);
            if (detectedPattern != null)
            {
                _detectedPatterns[translationKey] = detectedPattern;
                Console.WriteLine($"📌 Detected pattern for '{translationKey}': {detectedPattern}");
            }
        }
        
        // PHASE 2: Convert all texts using detected patterns
        foreach (var (key, text) in texts)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    results[key] = text;
                    continue;
                }

                var translationKey = key.Contains('|') ? key.Split('|')[1] : null;
                var preferredPattern = translationKey != null && _detectedPatterns.ContainsKey(translationKey) 
                    ? _detectedPatterns[translationKey] 
                    : null;

                var converted = ConvertPlaceholdersToHandlebars(text, preferredPattern);
                results[key] = converted;
                
                if (converted != text)
                {
                    successCount++;
                    Console.WriteLine($"✅ Converted {key}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error converting key: {key} - {ex.Message}");
                results[key] = text;
            }
        }

        Console.WriteLine($"Batch conversion complete: {successCount} converted out of {texts.Count}");

        return results;
    }

    /// <summary>
    /// Detects if text already contains String.Append or String.Concat
    /// </summary>
    private string? DetectHandlebarsPattern(string text)
    {
        if (text.Contains("{{String.Append"))
            return "String.Append";
        if (text.Contains("{{String.Concat"))
            return "String.Concat";
        return null;
    }

    private string ConvertPlaceholdersToHandlebars(string text, string? preferredPattern = null)
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
            Console.WriteLine($"⚠️ Unknown placeholder: {placeholder}");
            return text;
        }

        return ConvertComplexText(text, matches, preferredPattern);
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
                Console.WriteLine($"🔧 Repaired malformed placeholder: Added ']' after [{placeholderName}]");
            }
        }

        // Pattern 2: Nested brackets like [[Placeholder]] -> [Placeholder]
        if (text.Contains("[["))
        {
            text = text.Replace("[[", "[").Replace("]]", "]");
            Console.WriteLine("🔧 Repaired double brackets in text");
        }

        if (text != original)
        {
            Console.WriteLine($"📝 Repaired text: {original} → {text}");
        }

        return text;
    }

    private string ConvertComplexText(string text, MatchCollection matches, string? preferredPattern = null)
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

        // Use detected pattern from reference language, or default to String.Concat
        var method = preferredPattern ?? "String.Concat";
        var concatenated = string.Join(" ", parts);
        
        Console.WriteLine($"🔧 Using {method} for conversion (detected: {preferredPattern ?? "none"})");
        
        return $"{{{{{method} {concatenated}}}}}";
    }

    private string EscapeQuotes(string text)
    {
        return text.Replace("\"", "\\\"");
    }
}
