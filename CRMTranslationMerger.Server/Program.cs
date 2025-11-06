using CRMTranslationMerger.Server.Services;
using CRMTranslationMerger.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddHttpClient<OpenAiService>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// AI Conversion Endpoint (Legacy single-text conversion)
app.MapPost("/api/convert-placeholder", async (AiRequest request, OpenAiService aiService) =>
{
    var (success, convertedText, error) = await aiService.ConvertPlaceholderAsync(
        request.Text,
        request.JsonContext,
        request.ExcelContext
    );

    return success
        ? Results.Ok(new AiResponse { Success = true, ConvertedText = convertedText ?? string.Empty })
        : Results.Ok(new AiResponse { Success = false, Error = error });
})
.WithName("ConvertPlaceholder");

// ENTERPRISE BATCH CONVERSION ENDPOINT - Supports AI-powered and pattern-only modes
// Processes ALL translations in one call - INTELLIGENT, EFFICIENT, COMPREHENSIVE
app.MapPost("/api/convert-batch", async (BatchConversionRequest request, OpenAiService aiService) =>
{
    var mode = request.Mode switch
    {
        "pattern" => OpenAiService.ConversionMode.PatternOnly,
        "ai" => OpenAiService.ConversionMode.AiPowered,
        "hybrid" => OpenAiService.ConversionMode.SmartHybrid,
        _ => OpenAiService.ConversionMode.SmartHybrid
    };
    
    var convertedTexts = await aiService.ConvertBatchAsync(
        request.Texts,
        mode,
        request.ApiKey,
        request.Endpoint,
        request.Model
    );
    
    return Results.Ok(convertedTexts);
})
.WithName("ConvertBatch");

// Fallback to index.html for client-side routing
app.MapFallbackToFile("index.html");

app.Run();

// Simple DTO for batch conversion request
public record BatchConversionRequest(
    Dictionary<string, string> Texts,
    string Mode = "hybrid", // "pattern", "ai", or "hybrid"
    string? ApiKey = null,
    string? Endpoint = null,
    string? Model = null
);