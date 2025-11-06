# Azure App Settings Setup for CRM Translation Merger
# Stores OpenAI API key as encrypted app setting (works on Free tier)

param(
    [Parameter(Mandatory=$false)]
    [string]$StaticWebAppName = "crm-translation-merger",
    
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroup = "crm-translation-rg",
    
    [Parameter(Mandatory=$true)]
    [string]$OpenAiApiKey
)

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  Azure App Settings Setup (Free Tier Compatible)" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Configuring Static Web App..." -ForegroundColor Yellow
Write-Host "  App Name:        $StaticWebAppName" -ForegroundColor Cyan
Write-Host "  Resource Group:  $ResourceGroup" -ForegroundColor Cyan
Write-Host ""

Write-Host "Setting OPENAI_API_KEY as encrypted app setting..." -ForegroundColor Yellow

az staticwebapp appsettings set `
    --name $StaticWebAppName `
    --resource-group $ResourceGroup `
    --setting-names "OPENAI_API_KEY=$OpenAiApiKey" `
    --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=====================================================" -ForegroundColor Green
    Write-Host "  Setup Complete!" -ForegroundColor Green
    Write-Host "=====================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "✅ API key configured as encrypted app setting" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Your API key is now stored securely in Azure" -ForegroundColor White
    Write-Host "  2. The function will automatically use it" -ForegroundColor White
    Write-Host "  3. Users can still provide their own keys via UI" -ForegroundColor White
    Write-Host ""
    Write-Host "Security Benefits:" -ForegroundColor Yellow
    Write-Host "  ✓ Secret encrypted at rest by Azure" -ForegroundColor Green
    Write-Host "  ✓ Only accessible by your function" -ForegroundColor Green
    Write-Host "  ✓ Not visible in browser or code" -ForegroundColor Green
    Write-Host "  ✓ Works on Free tier (no upgrade needed)" -ForegroundColor Green
    Write-Host ""
    Write-Host "To update the key in the future:" -ForegroundColor Yellow
    Write-Host "  .\setup-appsettings.ps1 -OpenAiApiKey 'new-key'" -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "❌ Failed to set app setting" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  1. Check that you're logged in: az account show" -ForegroundColor White
    Write-Host "  2. Verify the Static Web App exists:" -ForegroundColor White
    Write-Host "     az staticwebapp list --output table" -ForegroundColor Cyan
    Write-Host ""
    exit 1
}
