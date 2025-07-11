using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Dotnet.AI.Console.Application;

[McpServerToolType]
public class MyCliTools(UserContextManager userContextManager)
    : IMyCliTools
{
    [McpServerTool]
    [Description("Gets the current weather conditions for a specified city.")]
    public static async Task<string> GetWeather([Description("The city to get weather for, e.g., 'London' or 'New York'.")] string location)
    {
        System.Console.WriteLine($"\n[TOOL CALL: GetWeather] Fetching weather for: {location}...");
        await Task.Delay(500);

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
        System.Console.WriteLine($"\n[TOOL CALL: SetUserPreference] User: {userId}, Key: {key}, Value: {value}");
        try
        {
            var userContext = await userContextManager.GetOrCreateContextAsync(userId);
            userContext.Preferences[key] = value;
            await userContextManager.UpdateContextAsync(userContext);
            System.Console.WriteLine($"[TOOL CALL: SetUserPreference] Preference set successfully.");
            return true;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[TOOL CALL: SetUserPreference] Error setting preference: {ex.Message}");
            return false;
        }
    }

    [McpServerTool]
    [Description("Retrieves a user preference by key from their profile. Requires a userId.")]
    public async Task<string> GetUserPreference(string userId, string key)
    {
        System.Console.WriteLine($"\n[TOOL CALL: GetUserPreference] User: {userId}, Key: {key}");
        var userContext = await userContextManager.GetOrCreateContextAsync(userId);
        if (userContext.Preferences.TryGetValue(key, out var value))
        {
            System.Console.WriteLine($"[TOOL CALL: GetUserPreference] Preference found: {value}");
            return value;
        }
        System.Console.WriteLine($"[TOOL CALL: GetUserPreference] Preference '{key}' not found for user '{userId}'.");
        return "";
    }
}



