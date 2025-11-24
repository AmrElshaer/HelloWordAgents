using Azure;
using Azure.AI.Inference;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OllamaSharp;
using OllamaSharp.Models;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;
using ChatRole = Microsoft.Extensions.AI.ChatRole;

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
var gitHubPatToken = Environment.GetEnvironmentVariable("GITHUB_API_KEY");
var model = "microsoft/Phi-4-reasoning"; //Example: deepseek/DeepSeek-V3-0324
var instructions = """
                                    You are an Azure DevOps expert. You have access to the following tools to help you with your tasks. Use the tools as needed to answer user questions about Azure DevOps. Be sure to provide clear and concise answers.
                   """;

var agent = new ChatCompletionsClient(
    new Uri("https://models.github.ai/inference"),
    new AzureKeyCredential(gitHubPatToken),
    new AzureAIInferenceClientOptions()).AsIChatClient(model).CreateAIAgent(instructions: instructions, tools: tools.Cast<AITool>().ToArray())
    .AsBuilder()
    .Use(FunctionCallMiddleware)
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

async ValueTask<object?> FunctionCallMiddleware(AIAgent callingAgent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
{
    StringBuilder functionCallDetails = new();
    functionCallDetails.Append($"- Tool Call: '{context.Function.Name}'");
    if (context.Arguments.Count > 0)
    {
        functionCallDetails.Append($" (Args: {string.Join(",", context.Arguments.Select(x => $"[{x.Key} = {x.Value}]"))}");
    }

    return await next(context, cancellationToken);
}

