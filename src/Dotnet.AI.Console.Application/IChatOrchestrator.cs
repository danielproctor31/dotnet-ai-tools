namespace Dotnet.AI.Console.Application;

public interface IChatOrchestrator
{
    Task ProcessUserQuery(string userQuery, string userId);
}

