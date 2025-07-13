using System.ClientModel;
using Dotnet.AI.Context;
using Dotnet.AI.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;

namespace Dotnet.AI.Console.Application;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets(typeof(Program).Assembly)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();

        var openaiClient =
            new ChatClient(configuration["OpenAI:Model"], new ApiKeyCredential(configuration["OpenAI:Key"]), new OpenAIClientOptions()
                {
                    Endpoint = new Uri(configuration["OpenAI:BaseUrl"]),
                })
                .AsIChatClient();

        var client = new ChatClientBuilder(openaiClient)
            .UseFunctionInvocation() // Enable function invocation for tool calls
            .Build();

        services.AddSingleton(client);

        // Add and configure the .NET MCP Server (example, not used for this console app)
        services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        services.AddSingleton<IUserContextManager, UserContextManager>();
        services.AddSingleton<IAiChatTools, AiChatTools>();
        services.AddSingleton<IChatOrchestrator, ChatOrchestrator>();

        var serviceProvider = services.BuildServiceProvider();

        var orchestrator = serviceProvider.GetRequiredService<IChatOrchestrator>();
        var userContextManager = serviceProvider.GetRequiredService<IUserContextManager>();
        // Generate a simple GUID for the user ID.
        var currentUserId = Guid.NewGuid().ToString();

        System.Console.WriteLine($"Current User Session: {currentUserId}");

        while (true)
        {
            System.Console.Write($"[{currentUserId}] Your query: ");
            var input = System.Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (input.Equals("exit", StringComparison.CurrentCultureIgnoreCase)
                || input.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
            {
                break;
            }

            if (input.Equals("clear my context", StringComparison.CurrentCultureIgnoreCase))
            {
                await userContextManager.ClearContextAsync(currentUserId);
                System.Console.WriteLine($"Context for user '{currentUserId}' cleared from memory.");
                continue;
            }

            await orchestrator.ProcessUserQuery(input, currentUserId);
            System.Console.WriteLine("\n");
        }
    }
}
