using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace Dotnet.AI.Console.Application;

public class ChatOrchestrator : IChatOrchestrator
{
    private readonly IChatClient _chatClient;
    private readonly IMcpServer _mcpServer;
    private readonly UserContextManager _userContextManager;
    private readonly IMyCliTools _myCliTools;

    public ChatOrchestrator(
        IChatClient chatClient,
        IMcpServer mcpServer,
        UserContextManager userContextManager,
        IMyCliTools myCliTools)
    {
        _chatClient = chatClient;
        _mcpServer = mcpServer;
        _userContextManager = userContextManager;
        _myCliTools = myCliTools;
    }

    public async Task ProcessUserQuery(string userQuery, string userId)
    {
        var userContext = await _userContextManager.GetOrCreateContextAsync(userId);

        ChatOptions chatOptions = new()
        {
            Tools =
            [
                AIFunctionFactory.Create(MyCliTools.GetWeather, "get_weather"),
                AIFunctionFactory.Create(_myCliTools.SetUserPreference),
                AIFunctionFactory.Create(_myCliTools.GetUserPreference),
            ]
        };

        List<ChatMessage> chatHistory = [
            new(ChatRole.System, "You are a helpful AI assistant. Use the available tools to answer questions and perform actions.")
        ];

        chatHistory.AddRange(userContext.ConversationHistory);
        chatHistory.Add(new ChatMessage(ChatRole.User, userQuery));

        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine($"\n--- User Query ({userId}): {userQuery} ---");
        System.Console.WriteLine("Sending query to AI via IChatClient (with automated tool handling)...");
        System.Console.ResetColor();

        await foreach (var message in _chatClient.GetStreamingResponseAsync(chatHistory, chatOptions))
        {
            if (!string.IsNullOrEmpty(message.Text))
            {
                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.Write(message.Text);
                System.Console.ResetColor();
            }
            userContext.ConversationHistory.Add(new ChatMessage(ChatRole.System, message.Text));
        }

        System.Console.WriteLine($"\n--- Final OpenAI Response Delivered ---");
        await _userContextManager.UpdateContextAsync(userContext);
    }
}


