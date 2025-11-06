namespace CRMTranslationMerger.Client.Models;

public class CrmDocument
{
    public required ContentDocWrapper ContentDoc { get; set; }
}

public class ContentDocWrapper
{
    public required ContentData Content { get; set; }
}

public class ContentData
{
    public required Dictionary<string, Dictionary<string, string>> LocalizedContents { get; set; }
}

public class AiRequest
{
    public string? ApiKey { get; set; }
    public Dictionary<string, string>? Texts { get; set; }
    public string? Mode { get; set; }
}

public class AiResponse
{
    public Dictionary<string, string>? ConvertedTexts { get; set; }
    public string? Error { get; set; }
}
