using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Dotnet.AI.MCP.Server;

[McpServerToolType]
public class McpTools
{
    // example of an MCP tool
    [McpServerTool]
    [Description("Gets the current weather conditions for a specified city.")]
    public async Task<string> GetWeather([Description("The city to get weather for, e.g., 'London' or 'New York'.")] string location)
    {
        Console.WriteLine($"\n[TOOL CALL: GetWeather] Fetching weather for: {location}...");

        if (location.Contains("London", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new { Location = "London, UK", Temperature = "16°C", Conditions = "Partly Cloudy", Unit = "Celsius" });
        }

        if (location.Contains("New York", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new { Location = "New York, USA", Temperature = "25°C", Conditions = "Sunny", Unit = "Celsius" });
        }

        return JsonSerializer.Serialize(new { Location = location, Temperature = "N/A", Conditions = "Data not available", Unit = "N/A" });
    }
}



