namespace CRMTranslationMerger.Shared.Models;

public class MergeResult
{
    public bool Success { get; set; }
    public string? MergedJson { get; set; }
    public int TotalTranslations { get; set; }
    public int AddedTranslations { get; set; }
    public int UpdatedTranslations { get; set; }
    public List<string> Languages { get; set; } = new();
    public string? Error { get; set; }
}
