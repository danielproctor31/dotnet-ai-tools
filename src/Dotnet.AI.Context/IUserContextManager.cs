namespace Dotnet.AI.Context;

public interface IUserContextManager
{
    Task<UserContext> GetOrCreateContextAsync(string userId);
    Task UpdateContextAsync(UserContext context);
    Task ClearContextAsync(string userId);
}
