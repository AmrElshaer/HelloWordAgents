using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OllamaSharp;

var azureDevOpsApiKey = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
var mcpClient = await McpClient.CreateAsync(
    new StdioClientTransport(new()
    {
        Name = "AzureDevOps",
        Command = "npx",
        Arguments = ["-y", "@tiberriver256/mcp-server-azure-devops"],
        EnvironmentVariables = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "AZURE_DEVOPS_ORG_URL", "https://dev.azure.com/SaudiVTS" },
                    { "AZURE_DEVOPS_AUTH_METHOD", "pat" },
                    { "AZURE_DEVOPS_PAT", azureDevOpsApiKey },
                    {"AZURE_DEVOPS_DEFAULT_PROJECT", "TasheerConnectV2" }
                }

    }));


Console.WriteLine("Available tools:");
IList<McpClientTool> tools = await mcpClient.ListToolsAsync();
foreach (var tool in tools)
{
    Console.WriteLine($"- {tool.Name}: {tool.Description}");
}

// Initialize agent



var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(30), // increase timeout,
    BaseAddress = new Uri("http://localhost:11434/") // Ollama server address

};

var client = new OllamaApiClient(httpClient, defaultModel: "llama3.1:latest");
var instructions = """
                                    You are an Azure DevOps expert. You have access to the following tools to help you with your tasks. Use the tools as needed to answer user questions about Azure DevOps. Be sure to provide clear and concise answers.
                   """;
var agent=client.CreateAIAgent(instructions: instructions, tools: tools.Cast<AITool>().ToArray())
    .AsBuilder()
    .Build();

AgentThread thread = agent.GetNewThread();
while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    ChatMessage message = new(ChatRole.User, input);
    AgentRunResponse response = await agent.RunAsync(message, thread);

    Console.WriteLine(response);
}



