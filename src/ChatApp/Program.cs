using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

try
{
    Console.WriteLine("Starting chat application...");

    var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile("local.appsettings.json", optional: true)
        .Build();

    var agentEndpoint = config["agentEndpoint"];
    if (string.IsNullOrEmpty(agentEndpoint))
        throw new InvalidOperationException("Agent endpoint is not configured in appsettings.json.");

    Console.WriteLine($"Connecting to agent at {agentEndpoint.Substring(0, 64)}...");
    using var client = new HttpClient();
    var initialResponse = await client.GetAsync(agentEndpoint);
    if (!initialResponse.IsSuccessStatusCode)
        throw new HttpRequestException($"Failed to connect to agent: {initialResponse.ReasonPhrase}");

    if (!initialResponse.Headers.TryGetValues("x-ms-agent-channel-callback-url", out var callbackUrls))
        throw new InvalidOperationException("Agent did not provide a callback URL.");

    var callbackUrl = callbackUrls.First();
    Console.WriteLine("Connection established successfully.");
    Console.WriteLine("Chat with the agent (type 'exit' to quit): ");

    while (true)
    {        
        Console.Write("Input: ");
        var input = Console.ReadLine();

        if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Exiting chat...");
            break;
        }

        var message = new { role = "user", content = input };
        var agentResponse = await client.PostAsJsonAsync(callbackUrl, message);

        if (!agentResponse.IsSuccessStatusCode)
            throw new HttpRequestException($"Agent response failed: {agentResponse.ReasonPhrase}");

        var agentResponseJson = await agentResponse.Content.ReadFromJsonAsync<ChatResponse>();
        if (agentResponseJson == null)
            throw new InvalidOperationException("Received null response from agent.");

        Console.ForegroundColor = ConsoleColor.Green;        
        Console.WriteLine($"Agent: {agentResponseJson.Content}");
        Console.ResetColor();
    }
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"An error occurred: {ex.Message}");
    Console.ResetColor();
}
finally
{
    Console.WriteLine("Goodbye!");
}

class ChatResponse
{
    public required string Role { get; set; }
    public required string Content { get; set; }
}