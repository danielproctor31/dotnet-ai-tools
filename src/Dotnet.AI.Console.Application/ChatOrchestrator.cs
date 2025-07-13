using Dotnet.AI.Context;
using Dotnet.AI.Tools;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace Dotnet.AI.Console.Application;

public class ChatOrchestrator(
    IChatClient chatClient,
    IMcpServer mcpServer, // example usage when MCP server is needed
    IUserContextManager userContextManager,
    IAiChatTools chatTools)
    : IChatOrchestrator
{
    public async Task ProcessUserQuery(string userQuery, string userId)
    {
        var userContext = await userContextManager.GetOrCreateContextAsync(userId);

        ChatOptions chatOptions = new()
        {
            Tools =
            [
                AIFunctionFactory.Create(chatTools.GetWeather, "get_weather"),
                AIFunctionFactory.Create(chatTools.SetUserPreference),
                AIFunctionFactory.Create(chatTools.GetUserPreference),
            ]
        };

        List<ChatMessage> chatHistory = [
            new(ChatRole.System, "You are a helpful AI assistant. Use the available tools to answer questions and perform actions.")
        ];

        chatHistory.AddRange(userContext.ConversationHistory);
        chatHistory.Add(new ChatMessage(ChatRole.User, userQuery));

        System.Console.WriteLine($"\n--- User Query ({userId}): {userQuery} ---");
        System.Console.WriteLine("Sending query to AI via IChatClient (with automated tool handling)...");

        await foreach (var message in chatClient.GetStreamingResponseAsync(chatHistory, chatOptions))
        {
            if (!string.IsNullOrEmpty(message.Text))
            {
                System.Console.Write(message.Text);
            }
            userContext.ConversationHistory.Add(new ChatMessage(ChatRole.System, message.Text));
        }

        System.Console.WriteLine($"\n--- Final OpenAI Response Delivered ---");
        await userContextManager.UpdateContextAsync(userContext);
    }
}


