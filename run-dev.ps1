# Run Development Server
Write-Host 'Starting CRM Translation Merger...' -ForegroundColor Cyan

# Check for .env file
if (Test-Path '.env') {
    Write-Host 'Loading environment variables from .env...' -ForegroundColor Yellow
    Get-Content '.env' | ForEach-Object {
        if ($_ -match '^([^=]+)=(.*)$') {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            [Environment]::SetEnvironmentVariable($key, $value, 'Process')
            Write-Host "   Set $key" -ForegroundColor Green
        }
    }
} else {
    Write-Host 'No .env file found' -ForegroundColor Yellow
}

# Start server (which hosts the Blazor client)
Write-Host '
Starting Server (hosting Blazor client)...' -ForegroundColor Cyan
Set-Location CRMTranslationMerger.Server
Start-Process powershell -ArgumentList '-NoExit', '-Command', 'dotnet watch run'

Write-Host '
Server starting...' -ForegroundColor Green
Write-Host '   Open: https://localhost:7125' -ForegroundColor Yellow
Write-Host '   (The Blazor client will be served from the server)' -ForegroundColor Gray
