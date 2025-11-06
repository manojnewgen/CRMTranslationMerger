# Test Complex CRM Scenarios
# Demonstrates AI handling of complex Handlebars expressions

$apiUrl = "https://icy-hill-069f16b10.3.azurestaticapps.net/api/convert-batch"

Write-Host "🧪 Testing Complex CRM Scenarios" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Your real-world scenarios
$complexScenarios = @{
    # Scenario 1: Already has Handlebars - should be preserved
    "existing_handlebars" = "{{LoadedData.SenderProfile.Handle}} likes you."
    
    # Scenario 2: Simple text - needs conversion
    "simple_conversion" = "[Sender name] likes you."
    
    # Scenario 3: Complex conditional with gender
    "gender_conditional" = "[Sender name] likes you. Message him/her for free"
    
    # Scenario 4: Email address with variable
    "email_format" = "[Sender name]@TalkMatch.com"
    
    # Scenario 5: Complex existing Handlebars with conditional
    "complex_existing" = "{{LoadedData.SenderProfile.Handle}} likes you. Message {{# if (String.Equal (Object.ToString localVars.gender) ""Female"")}} her {{else}} him {{/if}}for free"
}

Write-Host "📋 Test Scenarios:" -ForegroundColor Yellow
$complexScenarios.Keys | ForEach-Object {
    Write-Host "  - $_" -ForegroundColor Gray
}
Write-Host ""

# Test with Pattern Mode (will skip texts with existing Handlebars)
Write-Host "Test 1: Pattern Mode (fast, free)" -ForegroundColor Yellow
Write-Host "-----------------------------------" -ForegroundColor Gray

$body = @{
    Texts = $complexScenarios
    Mode = "pattern"
} | ConvertTo-Json -Depth 5

try {
    $response = Invoke-RestMethod -Uri $apiUrl -Method Post -Body $body -ContentType "application/json"
    Write-Host "✅ Success!" -ForegroundColor Green
    Write-Host ""
    
    foreach ($key in $response.PSObject.Properties.Name) {
        $original = $complexScenarios[$key]
        $converted = $response.$key
        
        Write-Host "Scenario: $key" -ForegroundColor Cyan
        Write-Host "  Original : $original" -ForegroundColor Gray
        Write-Host "  Converted: $converted" -ForegroundColor Green
        
        if ($converted -eq $original) {
            Write-Host "  Status   : Preserved (already Handlebars)" -ForegroundColor Yellow
        } else {
            Write-Host "  Status   : Converted" -ForegroundColor Green
        }
        Write-Host ""
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "💡 Key Insights:" -ForegroundColor Cyan
Write-Host "  • Pattern mode preserves existing Handlebars expressions" -ForegroundColor Gray
Write-Host "  • Simple placeholders like [Sender name] get converted" -ForegroundColor Gray
Write-Host "  • Complex conditionals need AI mode for proper handling" -ForegroundColor Gray
Write-Host ""
Write-Host "🤖 For complex scenarios with conditionals (him/her), use AI mode:" -ForegroundColor Yellow
Write-Host "   Mode = 'ai' or Mode = 'hybrid' (with OpenAI API key)" -ForegroundColor Gray
Write-Host ""
Write-Host "📝 Your complex Subject2 example will work best with AI mode," -ForegroundColor Cyan
Write-Host "   as it can intelligently handle gender-based conditionals." -ForegroundColor Cyan
