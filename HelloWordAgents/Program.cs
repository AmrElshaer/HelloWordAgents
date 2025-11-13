
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OllamaSharp;
var apiKey = Environment.GetEnvironmentVariable("GITHUB_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("❌ Missing GITHUB_API_KEY environment variable.");
    return;
}

var mcpClient = await McpClient.CreateAsync(
    new StdioClientTransport(new()
    {
        Name = "GitHub",
        Command = "npx",
        Arguments = ["-y", "@modelcontextprotocol/server-github"],
        EnvironmentVariables = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "GITHUB_PERSONAL_ACCESS_TOKEN", apiKey }
                }

    }));
// List all available tools from the MCP server.
Console.WriteLine("Available tools:");
IList<McpClientTool> tools = await mcpClient.ListToolsAsync();
// Initialize agent

// 🔹 Add your text prompt
var instructions = """


        You are a professional content strategist and visual analyst.

        You are given several related images. Your task is to:
        1. Carefully analyze each image for key details, themes, colors, objects, text, diagrams, or visual messages.
        2. Identify the common narrative or idea that connects all these images.
        3. Write a powerful and engaging post that:
           - Clearly explains what the images represent.
           - Highlights the insights, lessons, or story they convey.
           - Uses a professional but inspiring tone suitable for LinkedIn or a tech blog.
           - Includes a concise summary or key takeaway at the end.
        4. If the images contain technical or architectural diagrams, provide clear interpretations in plain language.
        5. The post should feel natural, insightful, and add value to professionals in the field.
        6. Images not give in order so consider this the post more organized and meanfull
        7. Commit the blog post as a markdown to AmrElshaer/HelloWordAgents with commit message: Blog post published {DateTime.Now:yyyy-MM-dd HH:mm:ss}

        Output format:
        ---
        **Title:** [A short, catchy title]
        **Post:**
        [Your detailed, well-written post with clear flow and insights]
        ---
       
    """;
IChatClient client = new OllamaApiClient(new Uri("http://localhost:11434"), "phi4-mini:latest");
ChatClientAgent agent = new(client,
     new ChatClientAgentOptions
     {
         Name = "Writer Blogs from images",
         Instructions = instructions,
         ChatOptions = new ChatOptions
         {
             Tools = [
               ..tools.Cast<AITool>()
            ],
         }
     }


    );


var result = await agent.RunAsync("describe solid principles");
Console.WriteLine(result.Text);

// 🔹 Load all image files from "images" folder
string imageFolder = Path.Combine(Directory.GetCurrentDirectory(), "images");
if (!Directory.Exists(imageFolder))
{
    Console.WriteLine($"❌ Folder not found: {imageFolder}");
    return;
}

var imageFiles = Directory.GetFiles(imageFolder, "*.*", SearchOption.TopDirectoryOnly)
    .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
    .ToList();

if (imageFiles.Count == 0)
{
    Console.WriteLine("⚠️ No images found in folder.");
    return;
}

Console.WriteLine($"Found {imageFiles.Count} images. Sending to model...");

// 🔹 Read all images and attach them as DataContent
var contents = imageFiles.Select(file =>
{
    var bytes = File.ReadAllBytes(file);
    var mimeType = file.EndsWith(".png") ? "image/png" : "image/jpeg";
    return new DataContent(bytes, mimeType);
}).ToList<AIContent>();




// 🔹 Build message
var message = new ChatMessage(ChatRole.User, contents);

// 🔹 Run the agent
var response = await agent.RunAsync(message);



Console.WriteLine("✅ Model Response:");
Console.WriteLine(response);
