# Test Azure Function API
# Tests the deployed /api/convert-batch endpoint

$apiUrl = "https://icy-hill-069f16b10.3.azurestaticapps.net/api/convert-batch"

Write-Host "🧪 Testing Azure Function API..." -ForegroundColor Cyan
Write-Host "URL: $apiUrl" -ForegroundColor Gray
Write-Host ""

# Test 1: Pattern Mode (no API key needed)
Write-Host "Test 1: Pattern Mode (simple conversion)" -ForegroundColor Yellow

$body = @{
    Texts = @{
        "test1" = "Hi [Sender name], welcome!"
        "test2" = "You are [Sender age] years old"
    }
    Mode = "pattern"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri $apiUrl -Method Post -Body $body -ContentType "application/json"
    Write-Host "✅ Success!" -ForegroundColor Green
    Write-Host "Results:" -ForegroundColor Gray
    $response | ConvertTo-Json -Depth 5
    Write-Host ""
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "---" -ForegroundColor Gray
Write-Host ""

# Test 2: Complex text detection
Write-Host "Test 2: Hybrid Mode (complexity detection)" -ForegroundColor Yellow

$body2 = @{
    Texts = @{
        "simple" = "[Sender name]"
        "complex" = "Hi [Sender name], your message was sent [TimeAgo]. You have [Recipient name] waiting."
    }
    Mode = "hybrid"
} | ConvertTo-Json

try {
    $response2 = Invoke-RestMethod -Uri $apiUrl -Method Post -Body $body2 -ContentType "application/json"
    Write-Host "✅ Success!" -ForegroundColor Green
    Write-Host "Results:" -ForegroundColor Gray
    $response2 | ConvertTo-Json -Depth 5
    Write-Host ""
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

Write-Host "🎉 Testing complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📝 Note: AI mode requires an OpenAI API key in the request body." -ForegroundColor Cyan
