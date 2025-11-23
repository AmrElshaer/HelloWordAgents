using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
var apiKey = Environment.GetEnvironmentVariable("GITHUB_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("❌ Missing GITHUB_API_KEY environment variable.");
    return;
}

//var mcpClient = await McpClient.CreateAsync(
//    new StdioClientTransport(new()
//    {
//        Name = "GitHub",
//        Command = "npx",
//        Arguments = ["-y", "@modelcontextprotocol/server-github"],
//        EnvironmentVariables = new Dictionary<string, string>(StringComparer.Ordinal)
//                {
//                    { "GITHUB_PERSONAL_ACCESS_TOKEN", apiKey }
//                }

//    }));
// List all available tools from the MCP server.
//Console.WriteLine("Available tools:");
//IList<McpClientTool> tools = await mcpClient.ListToolsAsync();
//foreach (var tool in  tools)
//{
//    Console.WriteLine($"- {tool.Name}: {tool.Description}");
//}

// Initialize agent



var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(30), // increase timeout,
    BaseAddress = new Uri("http://localhost:11434") // Ollama server address

};

var client = new OllamaApiClient(client:httpClient, defaultModel:"llama3.2-vision");
ChatClientAgent agent = new(client,
    new ChatClientAgentOptions
    {
        Instructions = "Extract data from images",
        //ChatOptions = new ChatOptions
        //{
        //    Tools = [..tools.Cast<AITool>()],
        //}
    });



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
// convert imageFiles to base64 strings
var data= new List<string>();
foreach (var imageFile in imageFiles)
    {
    byte[] imageBytes = File.ReadAllBytes(imageFile);
    string base64String = Convert.ToBase64String(imageBytes);
    
    data.Add($"data:image/{Path.GetExtension(imageFile).TrimStart('.')};base64,{base64String}");
}

Console.WriteLine($"Found {imageFiles.Count} images. Sending to model...");




var systemMessage = new ChatMessage(ChatRole.System, "You are a helpful assistant that extracts data from images.");

var userPrompt = new ChatMessage(ChatRole.User, "Extract the data from the images that user will give you");
while (true)
{
    
    var imagePath = Console.ReadLine();
    if (string.IsNullOrEmpty(imagePath))
    {
        break;
    }
    if (string.IsNullOrEmpty(imagePath)
        || !File.Exists(imagePath))
    {
        Console.WriteLine("❌ Invalid image path. Try again or press Enter to exit.");
        continue;
    }
    var imageBytes = File.ReadAllBytes(imagePath);
    string base64String = Convert.ToBase64String(imageBytes);


    var userImages = new ChatMessage(ChatRole.User, $"image: {base64String}");
    // 🔹 Run the agent
    await foreach (var response in agent.RunStreamingAsync([systemMessage, userPrompt, userImages], cancellationToken: CancellationToken.None))
    {
        Console.Write(response.Text);
    }
    Console.WriteLine(); // New line after response

}
//var userImages = new ChatMessage(ChatRole.User, $"images: {string.Join(',', data)} ");




//// 🔹 Run the agent
//await foreach(var response in  agent.RunStreamingAsync([systemMessage, userPrompt, userImages],cancellationToken: CancellationToken.None))
//{
//    Console.Write(response.Text);
//}


