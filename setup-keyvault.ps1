# Azure Key Vault Setup for CRM Translation Merger
# Sets up Azure Key Vault with Managed Identity for secure API key storage

param(
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "rg-crm-translation",
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus",
    
    [Parameter(Mandatory=$false)]
    [string]$KeyVaultName = "kv-crm-translation",
    
    [Parameter(Mandatory=$false)]
    [string]$StaticWebAppName = "crm-translation-merger",
    
    [Parameter(Mandatory=$false)]
    [string]$StaticWebAppResourceGroup = "crm-translation-rg",
    
    [Parameter(Mandatory=$true)]
    [string]$OpenAiApiKey,
    
    [Parameter(Mandatory=$false)]
    [string]$SecretName = "OPENAI-API-KEY"
)

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  Azure Key Vault Setup for CRM Translation Merger" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Create Resource Group (if doesn't exist)
Write-Host "[1/6] Checking Resource Group..." -ForegroundColor Yellow
$rgExists = az group exists --name $ResourceGroupName | ConvertFrom-Json
if (-not $rgExists) {
    Write-Host "Creating Resource Group: $ResourceGroupName in $Location" -ForegroundColor Green
    az group create --name $ResourceGroupName --location $Location
} else {
    Write-Host "Resource Group '$ResourceGroupName' already exists" -ForegroundColor Green
}
Write-Host ""

# Step 2: Create Key Vault
Write-Host "[2/6] Creating Key Vault..." -ForegroundColor Yellow
Write-Host "Key Vault Name: $KeyVaultName" -ForegroundColor Cyan

$kvCheck = az keyvault show --name $KeyVaultName --resource-group $ResourceGroupName 2>$null
if (-not $kvCheck) {
    Write-Host "Creating Key Vault..." -ForegroundColor Green
    az keyvault create --name $KeyVaultName --resource-group $ResourceGroupName --location $Location --enable-rbac-authorization false
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Key Vault created successfully" -ForegroundColor Green
    } else {
        Write-Host "Failed to create Key Vault" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Key Vault '$KeyVaultName' already exists" -ForegroundColor Green
}
Write-Host ""

# Step 3: Add OpenAI API Key as Secret
Write-Host "[3/6] Adding OpenAI API Key to Key Vault..." -ForegroundColor Yellow
az keyvault secret set --vault-name $KeyVaultName --name $SecretName --value $OpenAiApiKey --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "Secret '$SecretName' added successfully" -ForegroundColor Green
} else {
    Write-Host "Failed to add secret" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 4: Enable Managed Identity on Static Web App
Write-Host "[4/6] Enabling Managed Identity on Static Web App..." -ForegroundColor Yellow
$identity = az staticwebapp identity assign --name $StaticWebAppName --resource-group $StaticWebAppResourceGroup --query principalId --output tsv

if ($LASTEXITCODE -eq 0) {
    Write-Host "Managed Identity enabled" -ForegroundColor Green
    Write-Host "  Principal ID: $identity" -ForegroundColor Cyan
} else {
    Write-Host "Failed to enable Managed Identity" -ForegroundColor Red
    Write-Host "Note: You may need to find your Static Web App resource group first" -ForegroundColor Yellow
    Write-Host "Run: az staticwebapp list --output table" -ForegroundColor Yellow
    exit 1
}
Write-Host ""

# Step 5: Grant Key Vault Access to Managed Identity
Write-Host "[5/6] Granting Key Vault access to Managed Identity..." -ForegroundColor Yellow
az keyvault set-policy --name $KeyVaultName --object-id $identity --secret-permissions get list --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "Access policy configured (Get, List permissions)" -ForegroundColor Green
} else {
    Write-Host "Failed to set access policy" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 6: Configure Environment Variables
Write-Host "[6/6] Configuring Static Web App environment variables..." -ForegroundColor Yellow
$keyVaultUrl = "https://$KeyVaultName.vault.azure.net/"

Write-Host "Setting KEYVAULT_URL and OPENAI_SECRET_NAME..." -ForegroundColor Cyan

az staticwebapp appsettings set --name $StaticWebAppName --resource-group $StaticWebAppResourceGroup --setting-names "KEYVAULT_URL=$keyVaultUrl" "OPENAI_SECRET_NAME=$SecretName" --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "Environment variables configured" -ForegroundColor Green
} else {
    Write-Host "Failed to set environment variables" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Summary
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration Summary:" -ForegroundColor Yellow
Write-Host "  Resource Group:       $ResourceGroupName" -ForegroundColor White
Write-Host "  Key Vault:            $KeyVaultName" -ForegroundColor White
Write-Host "  Key Vault URL:        $keyVaultUrl" -ForegroundColor White
Write-Host "  Secret Name:          $SecretName" -ForegroundColor White
Write-Host "  Static Web App:       $StaticWebAppName" -ForegroundColor White
Write-Host "  Managed Identity ID:  $identity" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Commit and push your code changes (Key Vault integration)" -ForegroundColor White
Write-Host "  2. Wait for GitHub Actions deployment (~3-5 min)" -ForegroundColor White
Write-Host "  3. Test the API without providing an API key in the request" -ForegroundColor White
Write-Host "  4. The function will automatically fetch the key from Key Vault" -ForegroundColor White
Write-Host ""
Write-Host "Cost Estimate:" -ForegroundColor Yellow
Write-Host "  Key Vault:            ~`$0.03 per 10,000 operations" -ForegroundColor White
Write-Host "  Managed Identity:     Free (included with Azure services)" -ForegroundColor White
Write-Host "  Static Web App:       Free tier (already provisioned)" -ForegroundColor White
Write-Host ""
Write-Host "Security Benefits:" -ForegroundColor Yellow
Write-Host "  - No secrets in code or browser" -ForegroundColor Green
Write-Host "  - Centralized key rotation (update Key Vault, no code changes)" -ForegroundColor Green
Write-Host "  - Audit logs for API access" -ForegroundColor Green
Write-Host "  - Managed identity authentication (no credentials to manage)" -ForegroundColor Green
Write-Host ""
