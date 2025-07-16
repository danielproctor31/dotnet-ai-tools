using System.ClientModel;
using Dotnet.AI.Orchestration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using OpenAI;
using OpenAI.Chat;

namespace Dotnet.AI.MCPClient.Console;

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

        // Register the MCP project as a client
        services.AddSingleton<IMcpClient>(sp =>
        {
            var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = "MyMcpServer",
                Command = "dotnet",
                Arguments = ["run", "--project", "../Dotnet.AI.MCP.Server/Dotnet.AI.MCP.Server.csproj"],
            });

            // TODO alternatively register over http
            return McpClientFactory.CreateAsync(clientTransport).Result;
        });

        services.AddSingleton(client);

        services.AddSingleton<IChatOrchestrator, ChatOrchestrator>();

        var serviceProvider = services.BuildServiceProvider();

        var orchestrator = serviceProvider.GetRequiredService<IChatOrchestrator>();
        await orchestrator.Start();
    }
}
