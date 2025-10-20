using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
var apiKey=Environment.GetEnvironmentVariable("GITHUB_API_KEY");
IChatClient chatClient =
    new ChatClient(
            "gpt-4o-mini",
            new ApiKeyCredential(apiKey!),
            new OpenAIClientOptions { Endpoint = new Uri("https://models.github.ai/inference") })
  
        .AsIChatClient()
        .AsBuilder()
        .UseFunctionInvocation()
        .Build();
var mcpClient = await McpClient.CreateAsync(
    new StdioClientTransport(new()
    {
     
        Command = "dotnet run",
        Arguments = ["--project", "C:\\projects\\personal_projects\\McpServerProject\\McpServerProject\\McpServerProject.csproj"],
        Name = "Mcp server sample",
        EnvironmentVariables = new Dictionary<string, string>
    {
        { "WEATHER_CHOICES", "sunny,rainy,cloudy,snowy,windy" }
    }
    }));
// List all available tools from the MCP server.
Console.WriteLine("Available tools:");
IList<McpClientTool> tools = await mcpClient.ListToolsAsync();
foreach (McpClientTool tool in tools)
{
    Console.WriteLine($"{tool}");
}
Console.WriteLine();

// Conversational loop that can utilize the tools via prompts.
List<Microsoft.Extensions.AI.ChatMessage> messages = [];
while (true)
{
    Console.Write("Prompt: ");
    messages.Add(new(ChatRole.User, Console.ReadLine()));

    List<ChatResponseUpdate> updates = [];
    await foreach (ChatResponseUpdate update in chatClient
        .GetStreamingResponseAsync(messages, new() { Tools = [.. tools] }))
    {
        Console.Write(update);
        updates.Add(update);
    }
    Console.WriteLine();

    messages.AddMessages(updates);
}