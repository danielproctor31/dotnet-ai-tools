using System.ComponentModel;
using System.Text.Json;
using Dotnet.AI.Context;
using ModelContextProtocol.Server;

namespace Dotnet.AI.Tools;

[McpServerToolType]
public class AiChatTools(IUserContextManager userContextManager)
    : IAiChatTools
{
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

    [McpServerTool]
    [Description("Sets a user preference key-value pair in their profile. Requires a userId.")]
    public async Task<bool> SetUserPreference(string userId, string key, string value)
    {
        Console.WriteLine($"\n[TOOL CALL: SetUserPreference] User: {userId}, Key: {key}, Value: {value}");
        try
        {
            var userContext = await userContextManager.GetOrCreateContextAsync(userId);
            userContext.Preferences[key] = value;
            await userContextManager.UpdateContextAsync(userContext);
            Console.WriteLine("[TOOL CALL: SetUserPreference] Preference set successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TOOL CALL: SetUserPreference] Error setting preference: {ex.Message}");
            return false;
        }
    }

    [McpServerTool]
    [Description("Retrieves a user preference by key from their profile. Requires a userId.")]
    public async Task<string> GetUserPreference(string userId, string key)
    {
        Console.WriteLine($"\n[TOOL CALL: GetUserPreference] User: {userId}, Key: {key}");
        var userContext = await userContextManager.GetOrCreateContextAsync(userId);
        if (userContext.Preferences.TryGetValue(key, out var value))
        {
            Console.WriteLine($"[TOOL CALL: GetUserPreference] Preference found: {value}");
            return value;
        }
        Console.WriteLine($"[TOOL CALL: GetUserPreference] Preference '{key}' not found for user '{userId}'.");
        return "";
    }
}



