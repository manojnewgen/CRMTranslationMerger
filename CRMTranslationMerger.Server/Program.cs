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

// ENTERPRISE BATCH CONVERSION ENDPOINT (NEW!)
// Processes ALL translations in one call - FAST, EFFICIENT, RELIABLE
app.MapPost("/api/convert-batch", async (Dictionary<string, string> texts, OpenAiService aiService) =>
{
    var convertedTexts = await aiService.ConvertBatchAsync(texts);
    return Results.Ok(convertedTexts);
})
.WithName("ConvertBatch");

// Fallback to index.html for client-side routing
app.MapFallbackToFile("index.html");

app.Run();
