using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace Dotnet.AI.Orchestration;

public class ChatOrchestrator(
    IChatClient chatClient,
    IMcpClient mcpClient)
    : IChatOrchestrator
{
    // Example of local tool function
    private Task<string> GetF1Result()
    {
        Console.WriteLine($"\n[TOOL CALL: GetF1Results] Fetching F1 result...");
        return Task.FromResult("Landon Norris won the most recent F1 race in Silverstone, UK.");
    }

    public async Task Start()
    {
        // Register local tools
        var localTools = new List<AITool>
        {
            AIFunctionFactory.Create(GetF1Result, "get_f1_result")
        };

        foreach (var tool in localTools)
        {
            Console.WriteLine($"Local Tool: {tool}");
        }

        // register mcp tools
        var mcpTools = await mcpClient.ListToolsAsync();
        foreach (var tool in mcpTools)
        {
            Console.WriteLine($"MCP Tool: {tool}");
        }
        Console.WriteLine();

        ChatOptions chatOptions = new()
        {
            Tools =
            [
                ..localTools,
                ..mcpTools,
            ]
        };

        List<ChatMessage> messages =
        [
            new(ChatRole.System,
                "You are a helpful AI assistant. Use the available tools to answer questions and perform actions."),
        ];

        while (true)
        {
            Console.Write("Prompt: ");
            var prompt = Console.ReadLine();
            messages.Add(new ChatMessage(ChatRole.User, prompt));

            List<ChatResponseUpdate> updates = [];

            await foreach (var update in chatClient
                               .GetStreamingResponseAsync(messages, chatOptions))
            {
                Console.Write(update);
                updates.Add(update);
            }
            Console.WriteLine();

            messages.AddMessages(updates);
        }
    }
}


