# Script for fixing SCADA tags configuration
Write-Host "=== SCADA Tags Diagnostics & Fix ===" -ForegroundColor Cyan
Write-Host ""

$settingsPath = "$env:APPDATA\Scada.Client\settings.json"

if (Test-Path $settingsPath) {
    Write-Host "Found settings.json: $settingsPath" -ForegroundColor Yellow
    
    $content = Get-Content $settingsPath -Raw | ConvertFrom-Json
    $tagCount = if ($content.Tags) { $content.Tags.Count } else { 0 }
    
    Write-Host "Tags count: $tagCount" -ForegroundColor Yellow
    
    if ($tagCount -eq 0) {
        Write-Host ""
        Write-Host "Problem: No tags found!" -ForegroundColor Red
        Write-Host "Removing settings.json..." -ForegroundColor Yellow
        Remove-Item $settingsPath -Force
        Write-Host "Done! Restart application to load tags from tags.json" -ForegroundColor Green
    } elseif ($tagCount -lt 100) {
        Write-Host ""
        Write-Host "Showing current tags:" -ForegroundColor Cyan
        $content.Tags | Select-Object Name, Address, Register, Enabled | Format-Table
        
        Write-Host ""
        Write-Host "WARNING: Only $tagCount tags found (expected ~196000 from tags.json)" -ForegroundColor Yellow
        Write-Host ""
        $choice = Read-Host "Remove settings.json to reload from tags.json? (y/n)"
        
        if ($choice -eq 'y' -or $choice -eq 'Y') {
            Remove-Item $settingsPath -Force
            Write-Host "Settings removed! Restart application." -ForegroundColor Green
        } else {
            Write-Host "Keeping current settings." -ForegroundColor Yellow
        }
    } else {
        Write-Host ""
        Write-Host "Tags loaded OK ($tagCount items)" -ForegroundColor Green
    }
} else {
    Write-Host "settings.json not found." -ForegroundColor Yellow
    Write-Host "Will be created on first run." -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Complete ===" -ForegroundColor Cyan