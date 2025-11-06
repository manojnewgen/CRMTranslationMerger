<#
.SYNOPSIS
    Tests the Azure Function API with Key Vault integration
    
.DESCRIPTION
    Sends a test request WITHOUT an API key to verify that the function
    successfully retrieves the key from Azure Key Vault using Managed Identity
    
.NOTES
    Run this AFTER deploying the updated function with Key Vault integration
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$ApiUrl = "https://icy-hill-069f16b10.3.azurestaticapps.net/api/convert-batch",
    
    [Parameter(Mandatory=$false)]
    [string]$TestText = "Hello [Sender name]! You have [TimeAgo] message.",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("pattern", "ai", "hybrid")]
    [string]$Mode = "ai"
)

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  Testing Azure Key Vault Integration" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "API Endpoint: $ApiUrl" -ForegroundColor Yellow
Write-Host "Test Text:    $TestText" -ForegroundColor Yellow
Write-Host "Mode:         $Mode" -ForegroundColor Yellow
Write-Host ""
Write-Host "⚡ Sending request WITHOUT API key (should use Key Vault)..." -ForegroundColor Yellow
Write-Host ""

$body = @{
    Texts = @{
        "test-key" = $TestText
    }
    Mode = $Mode
} | ConvertTo-Json -Compress

try {
    $response = Invoke-RestMethod `
        -Uri $ApiUrl `
        -Method POST `
        -ContentType "application/json" `
        -Body $body `
        -TimeoutSec 30
    
    Write-Host "✅ SUCCESS!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Response:" -ForegroundColor Yellow
    Write-Host ($response | ConvertTo-Json -Depth 5) -ForegroundColor White
    Write-Host ""
    Write-Host "Converted Text:" -ForegroundColor Cyan
    Write-Host $response.'test-key' -ForegroundColor White
    Write-Host ""
    
    # Verify conversion happened
    if ($response.'test-key' -and $response.'test-key' -ne $TestText) {
        Write-Host "✅ Conversion successful - text was transformed" -ForegroundColor Green
        
        # Check if it looks like valid Handlebars
        if ($response.'test-key' -match '\{\{.*\}\}') {
            Write-Host "✅ Output contains Handlebars expressions" -ForegroundColor Green
        }
    } else {
        Write-Host "⚠️ Warning: Text was not converted" -ForegroundColor Yellow
        Write-Host "   This might indicate an issue with the API key or AI call" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "❌ ERROR!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error Details:" -ForegroundColor Yellow
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.Exception.Response) {
        Write-Host ""
        Write-Host "HTTP Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Yellow
        
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response Body:" -ForegroundColor Yellow
            Write-Host $responseBody -ForegroundColor Red
        } catch {
            # Ignore if we can't read response
        }
    }
    
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  1. Verify Key Vault is created and contains OPENAI-API-KEY secret" -ForegroundColor White
    Write-Host "  2. Check Managed Identity is enabled on Static Web App" -ForegroundColor White
    Write-Host "  3. Confirm access policy grants 'get' permission to managed identity" -ForegroundColor White
    Write-Host "  4. Verify environment variables (KEYVAULT_URL, OPENAI_SECRET_NAME) are set" -ForegroundColor White
    Write-Host "  5. Check Azure Portal -> Static Web App -> Application Insights for logs" -ForegroundColor White
    Write-Host ""
    Write-Host "Run setup script if not done yet:" -ForegroundColor Yellow
    Write-Host "  .\setup-keyvault.ps1 -OpenAiApiKey 'sk-your-key-here'" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""
