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
    
    // Conversion mode cache to store AI results
    private readonly Dictionary<string, string> _conversionCache = new();

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
    
    public enum ConversionMode
    {
        PatternOnly,      // Fast, free, limited to simple placeholders
        AiPowered,        // Intelligent, handles complex scenarios
        SmartHybrid       // Auto-detect complexity and choose best approach
    }

    public async Task<Dictionary<string, string>> ConvertBatchAsync(
        Dictionary<string, string> texts, 
        ConversionMode mode = ConversionMode.SmartHybrid,
        string? apiKey = null,
        string? endpoint = null,
        string? model = null)
    {
        var results = new Dictionary<string, string>();
        var successCount = 0;
        
        _logger.LogInformation("Starting batch conversion of {Count} texts using {Mode} mode", texts.Count, mode);
        
        foreach (var (key, text) in texts)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    results[key] = text;
                    continue;
                }

                string converted;
                
                // Check cache first
                if (_conversionCache.TryGetValue(text, out var cachedResult))
                {
                    _logger.LogDebug("Using cached conversion for: {Text}", text);
                    converted = cachedResult;
                }
                else
                {
                    // Determine conversion strategy
                    var shouldUseAi = mode switch
                    {
                        ConversionMode.PatternOnly => false,
                        ConversionMode.AiPowered => true,
                        ConversionMode.SmartHybrid => IsComplexText(text),
                        _ => false
                    };

                    if (shouldUseAi && !string.IsNullOrEmpty(apiKey))
                    {
                        _logger.LogInformation("🤖 Using AI for complex conversion: {Text}", text);
                        var aiResult = await ConvertWithAiAsync(text, apiKey, endpoint, model);
                        converted = aiResult.Success ? aiResult.ConvertedText! : ConvertPlaceholdersToHandlebars(text);
                    }
                    else
                    {
                        _logger.LogDebug("📐 Using pattern matching for: {Text}", text);
                        converted = ConvertPlaceholdersToHandlebars(text);
                    }
                    
                    // Cache the result
                    _conversionCache[text] = converted;
                }
                
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
    
    /// <summary>
    /// Determines if text is complex enough to require AI processing
    /// </summary>
    private bool IsComplexText(string text)
    {
        // Check for indicators of complexity:
        // 1. Multiple placeholders with surrounding text
        // 2. Nested structures
        // 3. Conditional keywords
        // 4. Loop keywords
        // 5. Multiple sentences
        
        var placeholderCount = Regex.Matches(text, @"\[([^\]]+)\]").Count;
        var hasConditionals = Regex.IsMatch(text, @"\b(if|else|unless)\b", RegexOptions.IgnoreCase);
        var hasLoops = Regex.IsMatch(text, @"\b(each|for|loop)\b", RegexOptions.IgnoreCase);
        var hasMultipleSentences = text.Split('.', '!', '?').Length > 2;
        var hasNestedBrackets = text.Contains("[[") || Regex.IsMatch(text, @"\[([^\]]*\[)");
        
        return (placeholderCount > 2 && text.Length > 50) || 
               hasConditionals || 
               hasLoops || 
               hasMultipleSentences ||
               hasNestedBrackets;
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
    
    /// <summary>
    /// Comprehensive AI-powered conversion with intelligent prompt engineering
    /// </summary>
    private async Task<(bool Success, string? ConvertedText, string? Error)> ConvertWithAiAsync(
        string text,
        string apiKey,
        string? endpoint = null,
        string? model = null)
    {
        try
        {
            var apiEndpoint = endpoint ?? _configuration["OpenAI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
            var modelName = model ?? _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            
            var systemPrompt = BuildComprehensiveSystemPrompt();
            var userPrompt = BuildUserPrompt(text);
            
            var requestBody = new
            {
                model = modelName,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.1, // Low temperature for consistent, deterministic output
                max_tokens = 1000
            };
            
            var request = new HttpRequestMessage(HttpMethod.Post, apiEndpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };
            
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            
            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("AI API Error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return (false, null, $"API Error: {response.StatusCode}");
            }
            
            var jsonResponse = JsonDocument.Parse(responseContent);
            var convertedText = jsonResponse.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?.Trim();
            
            if (string.IsNullOrEmpty(convertedText))
            {
                return (false, null, "Empty response from AI");
            }
            
            // Validate the AI output
            if (IsValidHandlebarsOutput(convertedText))
            {
                _logger.LogInformation("✅ AI conversion successful: {Original} → {Converted}", text, convertedText);
                return (true, convertedText, null);
            }
            else
            {
                _logger.LogWarning("⚠️ AI produced invalid Handlebars, falling back to pattern matching");
                return (false, null, "Invalid Handlebars syntax from AI");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AI conversion");
            return (false, null, ex.Message);
        }
    }
    
    /// <summary>
    /// Builds comprehensive system prompt for AI that covers all edge cases
    /// </summary>
    private string BuildComprehensiveSystemPrompt()
    {
        return @"You are an expert Handlebars template converter for a CRM email system. Your task is to convert text with placeholders into valid Handlebars syntax.

AVAILABLE VARIABLES:
- LoadedData.SenderProfile.Handle (sender's name)
- LoadedData.SenderProfile.Age (sender's age)
- LoadedData.RecipientProfile.Handle (recipient's name)
- LoadedData.RecipientProfile.Age (recipient's age)
- LoadedData.TimeAgo (relative time, e.g., '2 hours ago')
- LoadedData.Time (absolute time)

CONVERSION RULES:
1. Single placeholder alone: {{variable}}
   Example: [Sender name] → {{LoadedData.SenderProfile.Handle}}

2. Multiple parts (text + placeholders): {{String.Concat ""text"" variable ""text""}}
   Example: Hi [Sender name], welcome! → {{String.Concat ""Hi "" LoadedData.SenderProfile.Handle "", welcome!""}}

3. Conditionals: {{#if condition}}...{{else}}...{{/if}}
   Example: {{#if LoadedData.SenderProfile.Age}}You are {{LoadedData.SenderProfile.Age}} years old{{else}}Age unknown{{/if}}

4. Loops: {{#each items}}...{{/each}}
   Example: {{#each LoadedData.Items}}{{this.name}} {{/each}}

5. Nested expressions: Maintain proper nesting and spacing

6. Escape quotes: Use \"" for quotes inside strings

7. Preserve original text formatting, punctuation, and spacing

8. Handle edge cases:
   - Malformed placeholders (missing brackets)
   - Multiple consecutive placeholders
   - Placeholders at start/end of text
   - Non-English characters (preserve them)
   - Special characters in text

OUTPUT REQUIREMENTS:
- Return ONLY the converted Handlebars expression
- No explanations, no markdown, no extra text
- Must be valid Handlebars syntax
- Preserve exact spacing and punctuation from original text";
    }
    
    /// <summary>
    /// Builds user prompt with the text to convert
    /// </summary>
    private string BuildUserPrompt(string text)
    {
        // Provide context about known placeholders
        var knownPlaceholders = string.Join(", ", _placeholderMappings.Keys);
        
        return $@"Convert this text to Handlebars syntax:

Input text: ""{text}""

Known placeholders: {knownPlaceholders}

Convert and return ONLY the Handlebars expression, nothing else.";
    }
    
    /// <summary>
    /// Validates that AI output is proper Handlebars syntax
    /// </summary>
    private bool IsValidHandlebarsOutput(string output)
    {
        // Basic validation checks
        if (string.IsNullOrWhiteSpace(output))
            return false;
        
        // Must start and end with {{ }}
        if (!output.StartsWith("{{") || !output.EndsWith("}}"))
            return false;
        
        // Check balanced braces
        var openCount = Regex.Matches(output, @"\{\{").Count;
        var closeCount = Regex.Matches(output, @"\}\}").Count;
        if (openCount != closeCount)
            return false;
        
        // Check for common syntax errors
        if (output.Contains("{{{{{") || output.Contains("}}}}}"))
            return false;
        
        // If it has String.Concat, verify basic structure
        if (output.Contains("String.Concat"))
        {
            // Should have at least one space after String.Concat
            if (!Regex.IsMatch(output, @"String\.Concat\s+"))
                return false;
        }
        
        return true;
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
