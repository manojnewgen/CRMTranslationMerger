namespace CRMTranslationMerger.Shared.Models;

public class AiResponse
{
    public bool Success { get; set; }
    public string ConvertedText { get; set; } = string.Empty;
    public string? Error { get; set; }
}
