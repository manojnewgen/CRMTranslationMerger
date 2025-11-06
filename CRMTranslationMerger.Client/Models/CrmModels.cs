namespace CRMTranslationMerger.Client.Models;

public class CrmDocument
{
    public ContentDocWrapper? ContentDoc { get; set; }
}

public class ContentDocWrapper
{
    public ContentData? Content { get; set; }
}

public class ContentData
{
    public Dictionary<string, Dictionary<string, string>>? LocalizedContents { get; set; }
}

public class AiRequest
{
    public string? ApiKey { get; set; }
    public string? Text { get; set; }
    public object? JsonContext { get; set; }
    public object? ExcelContext { get; set; }
    public Dictionary<string, string>? Texts { get; set; }
    public string? Mode { get; set; }
}

public class AiResponse
{
    public bool Success { get; set; }
    public string? ConvertedText { get; set; }
    public Dictionary<string, string>? ConvertedTexts { get; set; }
    public string? Error { get; set; }
}
