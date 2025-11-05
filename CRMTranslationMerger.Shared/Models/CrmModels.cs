using System.Text.Json.Serialization;

namespace CRMTranslationMerger.Shared.Models;

// Real CRM structure models
public class CrmDocument
{
    [JsonPropertyName("contentDoc")]
    public CrmContentDoc? ContentDoc { get; set; }
}

public class CrmContentDoc
{
    [JsonPropertyName("header")]
    public CrmHeader? Header { get; set; }
    
    [JsonPropertyName("content")]
    public CrmContent? Content { get; set; }
}

public class CrmHeader
{
    [JsonPropertyName("workspaceId")]
    public int WorkspaceId { get; set; }
    
    [JsonPropertyName("key")]
    public CrmKey? Key { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

public class CrmKey
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("stringKey")]
    public string StringKey { get; set; } = string.Empty;
}

public class CrmContent
{
    [JsonPropertyName("configuration")]
    public object? Configuration { get; set; }
    
    [JsonPropertyName("data")]
    public string? Data { get; set; }
    
    [JsonPropertyName("localizedContents")]
    public Dictionary<string, Dictionary<string, string>>? LocalizedContents { get; set; }
}
