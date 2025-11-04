# Test JSON deserialization with lowercase keys and proper enum values

$testJson = @'
{
  "tags": [
    {
      "name": "X0",
      "address": 0,
      "register": "Coils",
      "type": "Bool",
      "scale": 1.0,
      "offset": 0.0,
      "enabled": false
    },
    {
      "name": "AI0",
      "address": 0,
      "register": "Input",
      "type": "Int16",
      "scale": 1.0,
      "offset": 0.0,
      "enabled": false
    }
  ]
}
'@

Write-Host "Test JSON:"
Write-Host $testJson
Write-Host "`nParsing..."

$obj = $testJson | ConvertFrom-Json
Write-Host "`nParsed successfully!"
Write-Host "Number of tags: $($obj.tags.Count)"
Write-Host "`nFirst tag:"
$obj.tags[0] | Format-List

Write-Host "`nChecking if keys are lowercase:"
$testJson -match '"name":'
Write-Host "Has lowercase 'name': $($Matches.Count -gt 0)"
