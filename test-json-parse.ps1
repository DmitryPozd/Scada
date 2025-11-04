# Simple test to verify tags.json can be parsed

Write-Host "Reading tags.json..." -ForegroundColor Cyan
$jsonPath = "Scada.Client\bin\Debug\net8.0\tags.json"

if (Test-Path $jsonPath) {
    $content = Get-Content $jsonPath -Raw | ConvertFrom-Json
    Write-Host "✓ JSON parsed successfully!" -ForegroundColor Green
    Write-Host "  Description: $($content.description)"
    Write-Host "  Total tags: $($content.totalTags)"
    Write-Host "  Tags array count: $($content.tags.Count)"
    Write-Host ""
    Write-Host "First 3 tags:" -ForegroundColor Yellow
    for ($i = 0; $i -lt [Math]::Min(3, $content.tags.Count); $i++) {
        $tag = $content.tags[$i]
        Write-Host "  [$i] $($tag.name): addr=$($tag.address), reg=$($tag.register), type=$($tag.type)"
    }
} else {
    Write-Host "✗ File not found: $jsonPath" -ForegroundColor Red
}
