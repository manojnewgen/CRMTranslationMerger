namespace CRMTranslationMerger.Shared.Models;

public class AiRequest
{
    public string Text { get; set; } = string.Empty;
    public object? JsonContext { get; set; }
    public object? ExcelContext { get; set; }
}
