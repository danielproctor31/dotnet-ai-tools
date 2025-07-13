using System.Collections.Concurrent;

namespace Dotnet.AI.Context;

public class UserContextManager : IUserContextManager
{
    private readonly ConcurrentDictionary<string, UserContext> _contexts = new();

    public Task<UserContext> GetOrCreateContextAsync(string userId)
    {
        Console.WriteLine($"[ContextManager] Fetching/Creating context for user: {userId} (In-Memory)...");
        var context = _contexts.GetOrAdd(userId, new UserContext(userId));
        return Task.FromResult(context);
    }

    public Task UpdateContextAsync(UserContext context)
    {
        context.LastActive = DateTime.UtcNow;
        _contexts[context.UserId] = context;
        Console.WriteLine($"[ContextManager] Updated context for user: {context.UserId} (In-Memory).");
        return Task.CompletedTask;
    }

    public Task ClearContextAsync(string userId)
    {
        _contexts.TryRemove(userId, out _);
        Console.WriteLine($"[ContextManager] Cleared context for user: {userId} (In-Memory).");
        return Task.CompletedTask;
    }
}
