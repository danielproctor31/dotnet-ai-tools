// Program.cs

using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using ModelContextProtocol.Server;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

// --- User Context Management (In-Memory) ---
public class UserContext
{
    public string UserId { get; set; }
    // ChatMessage from Azure.AI.OpenAI
    public List<Microsoft.Extensions.AI.ChatMessage> ConversationHistory { get; set; } = [];
    public Dictionary<string, string> Preferences { get; set; } = new Dictionary<string, string>();
    public DateTime LastActive { get; set; }

    public UserContext(string userId)
    {
        UserId = userId;
        LastActive = DateTime.UtcNow;
    }
}

public class UserContextManager
{
    // In-memory storage. Data will be lost when the application restarts.
    private readonly ConcurrentDictionary<string, UserContext> _contexts = new ConcurrentDictionary<string, UserContext>();

    /// <summary>
    /// Retrieves an existing user context or creates a new one if it doesn't exist.
    /// </summary>
    /// <param name="userId">The unique identifier for the user.</param>
    /// <returns>The UserContext object for the given user ID.</returns>
    public Task<UserContext> GetOrCreateContextAsync(string userId)
    {
        Console.WriteLine($"[ContextManager] Fetching/Creating context for user: {userId} (In-Memory)...");
        var context = _contexts.GetOrAdd(userId, new UserContext(userId));
        return Task.FromResult(context);
    }

    /// <summary>
    /// Updates the user context in the store. Also updates the LastActive timestamp.
    /// </summary>
    /// <param name="context">The UserContext object to update.</param>
    public Task UpdateContextAsync(UserContext context)
    {
        context.LastActive = DateTime.UtcNow;
        _contexts[context.UserId] = context; // Update or overwrite
        Console.WriteLine($"[ContextManager] Updated context for user: {context.UserId} (In-Memory).");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears a specific user's context from the store.
    /// </summary>
    /// <param name="userId">The unique identifier for the user.</param>
    public Task ClearContextAsync(string userId)
    {
        _contexts.TryRemove(userId, out _);
        Console.WriteLine($"[ContextManager] Cleared context for user: {userId} (In-Memory).");
        return Task.CompletedTask;
    }
}


// --- Services/Tools (Defined using .NET MCP SDK attributes) ---
[McpServerToolType]
public class MyCliTools
{
    private readonly HttpClient _httpClient;
    private readonly UserContextManager _userContextManager; // Inject UserContextManager

    public MyCliTools(HttpClient httpClient, UserContextManager userContextManager)
    {
        _httpClient = httpClient;
        _userContextManager = userContextManager;
    }

    /// <summary>
    /// Gets the current weather conditions for a specified city.
    /// </summary>
    /// <param name="location">The city to get weather for, e.g., 'London' or 'New York'.</param>
    /// <returns>A JSON string containing weather information.</returns>
    [McpServerTool]
    [Description("Gets the current weather conditions for a specified city.")]
    public async Task<string> GetWeather([Description("The city to get weather for, e.g., 'London' or 'New York'.")] string location)
    {
        Console.WriteLine($"\n[TOOL CALL: GetWeather] Fetching weather for: {location}...");
        await Task.Delay(500);

        if (location.Contains("London", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new { Location = "London, UK", Temperature = "16°C", Conditions = "Partly Cloudy", Unit = "Celsius" });
        }
        else if (location.Contains("New York", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new { Location = "New York, USA", Temperature = "25°C", Conditions = "Sunny", Unit = "Celsius" });
        }
        else
        {
            return JsonSerializer.Serialize(new { Location = location, Temperature = "N/A", Conditions = "Data not available", Unit = "N/A" });
        }
    }

    /// <summary>
    /// Logs an arbitrary message to a persistent system audit log.
    /// </summary>
    /// <param name="message">The text message to log.</param>
    /// <param name="severity">The severity level of the log (e.g., 'Info', 'Warning', 'Error'). Defaults to 'Info'.</param>
    /// <returns>A boolean indicating success or failure of the logging operation.</returns>
    [McpServerTool]
    [Description("Logs an arbitrary message to a persistent system audit log.")]
    public async Task<bool> LogMessage([Description("The text message to log.")] string message,
                                       [Description("The severity level of the log (e.g., 'Info', 'Warning', 'Error').")] string severity = "Info")
    {
        Console.WriteLine($"\n[TOOL CALL: LogMessage] Severity: {severity}, Message: {message}");
        await Task.Delay(200);
        Console.WriteLine($"[TOOL CALL: LogMessage] Successfully logged.");
        return true;
    }

    /// <summary>
    /// Sets a user preference key-value pair in their profile.
    /// </summary>
    /// <param name="userId">The ID of the user whose preference to set.</param>
    /// <param name="key">The preference key (e.g., 'preferred_city', 'theme').</param>
    /// <param name="value">The preference value.</param>
    /// <returns>True if the preference was set successfully, false otherwise.</returns>
    [McpServerTool]
    [Description("Sets a user preference key-value pair in their profile. Requires a userId.")]
    public async Task<bool> SetUserPreference([Description("The ID of the user whose preference to set.")] string userId,
                                                [Description("The preference key (e.g., 'preferred_city', 'theme').")] string key,
                                                [Description("The preference value.")] string value)
    {
        Console.WriteLine($"\n[TOOL CALL: SetUserPreference] User: {userId}, Key: {key}, Value: {value}");
        try
        {
            var userContext = await _userContextManager.GetOrCreateContextAsync(userId);
            userContext.Preferences[key] = value;
            await _userContextManager.UpdateContextAsync(userContext);
            Console.WriteLine($"[TOOL CALL: SetUserPreference] Preference set successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TOOL CALL: SetUserPreference] Error setting preference: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Retrieves a user preference by key from their profile.
    /// </summary>
    /// <param name="userId">The ID of the user whose preference to retrieve.</param>
    /// <param name="key">The preference key to retrieve.</param>
    /// <returns>The preference value as a string, or an empty string if not found.</returns>
    [McpServerTool]
    [Description("Retrieves a user preference by key from their profile. Requires a userId.")]
    public async Task<string> GetUserPreference([Description("The ID of the user whose preference to retrieve.")] string userId,
                                                [Description("The preference key to retrieve.")] string key)
    {
        Console.WriteLine($"\n[TOOL CALL: GetUserPreference] User: {userId}, Key: {key}");
        var userContext = await _userContextManager.GetOrCreateContextAsync(userId);
        if (userContext.Preferences.TryGetValue(key, out var value))
        {
            Console.WriteLine($"[TOOL CALL: GetUserPreference] Preference found: {value}");
            return value;
        }
        Console.WriteLine($"[TOOL CALL: GetUserPreference] Preference '{key}' not found for user '{userId}'.");
        return "";
    }
}

// --- Main Application Logic ---
public class ChatOrchestrator
{
    private readonly IChatClient _chatClient; // Now using IChatClient from Microsoft.Extensions.AI
    private readonly IMcpServer _mcpServer; // Kept for consistency, though not strictly needed by ChatOrchestrator's direct logic now
    private readonly UserContextManager _userContextManager;
    private readonly MyCliTools _myCliTools; // Needed to create AIFunction instances

    public ChatOrchestrator(IChatClient chatClient, IMcpServer mcpServer, UserContextManager userContextManager, MyCliTools myCliTools)
    {
        _chatClient = chatClient;
        _mcpServer = mcpServer; // Still injected for completeness, not directly used for tool dispatch
        _userContextManager = userContextManager;
        _myCliTools = myCliTools; // Get instance to pass to AIFunctionFactory
    }

    public async Task ProcessUserQuery(string userQuery, string userId)
    {
        var userContext = await _userContextManager.GetOrCreateContextAsync(userId); // Await context retrieval
        
        ChatOptions chatOptions = new()
        {
            Tools =
            [
                AIFunctionFactory.Create(_myCliTools.GetWeather, "get_weather"),
                AIFunctionFactory.Create(_myCliTools.LogMessage),
                AIFunctionFactory.Create(_myCliTools.SetUserPreference),
                AIFunctionFactory.Create(_myCliTools.GetUserPreference),
            ]

        };
        
        // add the system message
        List<Microsoft.Extensions.AI.ChatMessage> chatHistory = [
                new(ChatRole.System, "You are a helpful AI assistant. Use the available tools to answer questions and perform actions.")
        ];
        
        // Add historical messages from user context
        chatHistory.AddRange(userContext.ConversationHistory);


        // Add the current user query
        chatHistory.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, userQuery));

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n--- User Query ({userId}): {userQuery} ---");
        Console.WriteLine("Sending query to AI via IChatClient (with automated tool handling)...");
        Console.ResetColor();

        // Use the streaming API for a better user experience and simpler tool chaining
        // The IChatClient handles the tool calls internally.
        // It processes tool calls and their results *before* yielding the final text output.
        await foreach (var message in _chatClient.GetStreamingResponseAsync(chatHistory, chatOptions))
        {
            // Only append content to console if it's text.
            if (!string.IsNullOrEmpty(message.Text))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(message.Text);
                Console.ResetColor();
            }

            // Add all messages (including internal tool calls/responses) to the history to maintain context.
            // The IChatClient might yield intermediate messages for debugging if configured,
            // but primarily focuses on the final text content.
            userContext.ConversationHistory.Add(new ChatMessage(ChatRole.System, message.Text));
        }

        // The loop finishes when the AI has provided its final response.
        Console.WriteLine($"\n--- Final OpenAI Response Delivered ---");

        // The token usage is not directly available per stream message.
        // If you need it, you would typically use the non-streaming GetResponseAsync()
        // which returns a ChatCompletions object with Usage property.

        // Update the user's context in the manager after the conversation turn is complete
        await _userContextManager.UpdateContextAsync(userContext);
    }
}

// --- Argument Classes for Deserialization (Matching Tool Schemas) ---
// These are primarily for type-safety when manually working with arguments,
// but the AIFunctionFactory handles conversion more directly.
public class GetWeatherArgs
{
    [JsonPropertyName("location")] // Use lowercase for consistency with AIFunctionFactory
    public string Location { get; set; } = string.Empty;
}

public class LogMessageArgs
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string? Severity { get; set; }
}

public class SetUserPreferenceArgs
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class GetUserPreferenceArgs
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
}


// --- Program Entry Point ---
public static partial class Program
{
    public static async Task Main(string[] args)
    {
        // 1. Setup Configuration
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets(typeof(Program).Assembly)
            .AddEnvironmentVariables()
            .Build();
        
        // 2. Setup Services using ServiceCollection
        var services = new ServiceCollection();

        // Add HTTP Client
        services.AddHttpClient();

        // Add User Context Manager (uses in-memory ConcurrentDictionary)
        services.AddSingleton<UserContextManager>();

        // Add our custom tools (MyCliTools depends on HttpClient and UserContextManager)
        // This concrete instance will be used by AIFunctionFactory to invoke methods.
        services.AddSingleton<MyCliTools>(); 

        IChatClient openaiClient =
            new ChatClient(configuration["OpenAI:Model"], new ApiKeyCredential(configuration["OpenAI:Key"]), new OpenAIClientOptions()
                {
                    Endpoint = new Uri(configuration["OpenAI:BaseUrl"]),
                })
                .AsIChatClient();

        IChatClient client = new ChatClientBuilder(openaiClient)
            .UseFunctionInvocation()
            .Build();
        
        services.AddSingleton(client);
        
        // Add and configure the .NET MCP Server (optional in this context, but good for standardization)
        services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();
        
        // Add the ChatOrchestrator
        services.AddSingleton<ChatOrchestrator>();

        // 3. Build ServiceProvider and resolve the orchestrator
        var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetRequiredService<ChatOrchestrator>();
        
        // Generate a simple GUID for the user ID, as there's no auth integration now.
        // This ID will be unique per application run, but context will reset on restart.
        string currentUserId = Guid.NewGuid().ToString(); 

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("---------------------------------------------");
        Console.WriteLine("  AI Agent with Microsoft.Extensions.AI.OpenAI");
        Console.WriteLine("     (Automated Tool Calling via IChatClient)");
        Console.WriteLine("     (Chained Tool Calls & IN-MEMORY Context)");
        Console.WriteLine("     (Simplified Console App Startup)");
        Console.WriteLine("---------------------------------------------\n");
        Console.WriteLine($"Current User Session: {currentUserId}");
        Console.WriteLine("Type 'exit' or 'quit' to end.");
        Console.WriteLine("Try asking things like:");
        Console.WriteLine("  - What's the weather in London?");
        Console.WriteLine("  - Tell me a joke.");
        Console.WriteLine("  - Log that 'User logged in to the application' with severity 'Info'.");
        Console.WriteLine("  - First log 'test chain call' then tell me the weather in New York.");
        Console.WriteLine("  - Set my preferred city to Paris."); // Context interaction (in-memory)
        Console.WriteLine("  - What is my preferred city?"); // Retrieve context (in-memory)
        Console.WriteLine("  - Clear my context."); // Clear context (in-memory)
        Console.WriteLine("---------------------------------------------\n");
        Console.ResetColor();

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write($"[{currentUserId}] Your query: ");
            Console.ResetColor();
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (input.ToLower() == "exit" || input.ToLower() == "quit")
            {
                break;
            }
            
            if (input.ToLower() == "clear my context")
            {
                await serviceProvider.GetRequiredService<UserContextManager>().ClearContextAsync(currentUserId);
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Context for user '{currentUserId}' cleared from memory.");
                Console.ResetColor();
                continue;
            }

            await orchestrator.ProcessUserQuery(input, currentUserId);
            Console.WriteLine("\n");
        }

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("Application ended. Goodbye!");
        Console.ResetColor();
    }
}
