# Test instructions for debugging tags loading issue

## Step 1: Build the project
```powershell
dotnet build Scada.Client
```

## Step 2: Test JSON parsing (PowerShell)
```powershell
.\test-json-parse.ps1
```

This will verify tags.json can be parsed by PowerShell ConvertFrom-Json.

## Step 3: Run the application
```powershell
dotnet run --project Scada.Client
```

## Step 4: Open Tags Editor
1. Click "Settings" button in main window
2. In Settings window, click "Edit Tags" button

## Expected Console Output:

When you open the Tags Editor, you should see:

```
=== TagsConfigService: Created
=== BaseDirectory: O:\Projects\Visual StudioCodeProjects\Scada\Scada.Client\bin\Debug\net8.0\
=== Tags file path: O:\Projects\Visual StudioCodeProjects\Scada\Scada.Client\bin\Debug\net8.0\tags.json
=== File exists: True
=== File size: 6444937 bytes
=== TagsEditorWindowViewModel: Constructor called
=== Active tags count: 0
=== TagsEditorWindowViewModel: Starting LoadAvailableTagsAsync...
=== LoadAvailableTagsAsync: Starting...
=== LoadConfigurationAsync: Method called
=== Tags file exists: O:\...\tags.json
=== Reading file...
=== File read. Length: 6444937 characters
=== Creating JsonSerializerOptions...
=== Deserializing...
=== Result is not null. Description: 'Complete PLC address mapping', TotalTags: 35421
=== SUCCESS: Loaded 35421 tags
===   Tag[0]: X0, Addr=0, Reg=Input, Type=Bool
===   Tag[1]: X1, Addr=1, Reg=Input, Type=Bool
===   Tag[2]: X2, Addr=2, Reg=Input, Type=Bool
=== LoadAvailableTagsAsync: Config loaded. Tags count = 35421
=== LoadAvailableTagsAsync: Active tags count = 0
=== LoadAvailableTagsAsync: Available tags after filtering = 35421
=== LoadAvailableTagsAsync: Added 100 tags to AvailableTags
```

## If you see errors:

1. **"Tags file not found"** - tags.json is missing from bin/Debug/net8.0/
2. **"EXCEPTION"** - JSON deserialization error (check enum values)
3. **"result.Tags is NULL"** - Tags property didn't deserialize
4. **No output at all** - TagsEditorWindow not opening or constructor not being called

## Quick diagnostic:
```powershell
# Check if file exists
Test-Path "Scada.Client\bin\Debug\net8.0\tags.json"

# Check file size
(Get-Item "Scada.Client\bin\Debug\net8.0\tags.json").Length
```
