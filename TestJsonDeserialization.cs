using System;
using System.Text.Json;
using Scada.Client.Models;
using Scada.Client.Services;

// Quick test to verify JSON deserialization works with lowercase keys

var testJson = @"{
  ""tags"": [
    {
      ""name"": ""X0"",
      ""address"": 0,
      ""register"": ""Coils"",
      ""type"": ""Bool"",
      ""scale"": 1.0,
      ""offset"": 0.0,
      ""enabled"": false
    },
    {
      ""name"": ""AI0"",
      ""address"": 0,
      ""register"": ""Input"",
      ""type"": ""Int16"",
      ""scale"": 1.0,
      ""offset"": 0.0,
      ""enabled"": false
    }
  ]
}";

Console.WriteLine("Testing JSON deserialization with lowercase keys...\n");

var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = 
    { 
        new CaseInsensitiveEnumConverter<RegisterType>(),
        new CaseInsensitiveEnumConverter<DataType>(),
        new CaseInsensitiveEnumConverter<WordOrder>()
    }
};

try
{
    var config = JsonSerializer.Deserialize<TagsConfiguration>(testJson, options);
    
    if (config?.Tags != null)
    {
        Console.WriteLine($"✓ Successfully loaded {config.Tags.Count} tags");
        
        foreach (var tag in config.Tags)
        {
            Console.WriteLine($"  - {tag.Name}: Address={tag.Address}, Register={tag.Register}, Type={tag.Type}");
        }
    }
    else
    {
        Console.WriteLine("✗ Deserialization returned null or empty Tags");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Error: {ex.Message}");
    Console.WriteLine($"  {ex.StackTrace}");
}
