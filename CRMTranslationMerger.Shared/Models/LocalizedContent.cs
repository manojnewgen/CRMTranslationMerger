namespace CRMTranslationMerger.Shared.Models;

public class LocalizedContent
{
    public string CultureCode { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ContentDoc
{
    public string Header { get; set; } = string.Empty;
    public ContentData Content { get; set; } = new();
}

public class ContentData
{
    public List<LocalizedContent> LocalizedContents { get; set; } = new();
}
