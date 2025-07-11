using System.Diagnostics.CodeAnalysis;

namespace Dotnet.AI.Console.Application;

[ExcludeFromCodeCoverage]
public class UserContext(string userId)
{
    public string UserId { get; set; } = userId;
    public List<Microsoft.Extensions.AI.ChatMessage> ConversationHistory { get; set; } = [];
    public Dictionary<string, string> Preferences { get; set; } = new();
    public DateTime LastActive { get; set; } = DateTime.UtcNow;
}
