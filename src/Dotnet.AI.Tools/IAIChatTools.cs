using System.ComponentModel;

namespace Dotnet.AI.Tools;

public interface IAiChatTools
{
    Task<string> GetWeather([Description("The city to get weather for, e.g., 'London' or 'New York'.")] string location);

    [Description("Sets a user preference key-value pair in their profile. Requires a userId.")]
    Task<bool> SetUserPreference(string userId, string key, string value);

    [Description("Retrieves a user preference by key from their profile. Requires a userId.")]
    Task<string> GetUserPreference(string userId, string key);
}



