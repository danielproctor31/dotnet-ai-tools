using System.ComponentModel;

namespace Dotnet.AI.Console.Application;

public interface IMyCliTools
{
    [Description("Sets a user preference key-value pair in their profile. Requires a userId.")]
    Task<bool> SetUserPreference(string userId, string key, string value);

    [Description("Retrieves a user preference by key from their profile. Requires a userId.")]
    Task<string> GetUserPreference(string userId, string key);
}



