using Microsoft.AspNetCore.SignalR;

namespace DurableAgentSignalR;

public class AgentHub : Hub
{
    private readonly ILogger<AgentHub> _logger;

    public AgentHub(ILogger<AgentHub> logger)
    {
        _logger = logger;
    }

    public async Task SendMessage(string user, string message)
    {
        _logger.LogInformation("Received Message: {user} - {message}", user, message);
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    //Should be authorised.
    public async Task NotifyAgentResponse(string chatIdentifier, string result)
    {
        _logger.LogInformation("Received Agent Response: {result}", result);
        await Clients.Client(chatIdentifier).SendAsync("ReceiveMessage", "Agent", result);
    }

    // ReSharper disable once UnusedMember.Global
    public async Task NotifyAgentStoryResponse(string chatIdentifier, string result)
    {
        _logger.LogInformation("Received Agent Story Response: {result}", result);
        await Clients.Client(chatIdentifier).SendAsync("ReceiveStoryMessage", result);
    }

    //Should be authorised.
    // ReSharper disable once UnusedMember.Global
    public async Task AskForUserInput(string chatIdentifier, string from, string question, string eventName)
    {
        _logger.LogInformation("Prompting user for information: {eventName}", eventName);
        await Clients.Client(chatIdentifier).SendAsync("AskQuestion", from, question, eventName);
    }

    // ReSharper disable once UnusedMember.Global
    public async Task UserResponse(string response, string eventName)
    {

        var client = new HttpClient();
        var instanceId = Context.Items["InstanceId"];
        
        _logger.LogInformation("Event name: {eventName}, InstanceId: {instanceId}", eventName, instanceId);
        
        await Clients.Caller.SendAsync("ReceiveMessage", "User", response);
        
        var res = await client.PostAsJsonAsync("http://localhost:7115/api/ResponseToQuestion",
            new
            {
                InstanceId = instanceId,
                EventName = eventName,
                Response = response
            });
    }

    public async Task NewChat()
    {
        //start a durable function orchestration over in function land
        var client = new HttpClient();
        var response = await client.PostAsJsonAsync("http://localhost:7115/api/StartNewChat",
            new
            {
                SignalrChatIdentifier = Context.ConnectionId
            });

        var instanceId = await response.Content.ReadAsStringAsync();
        if (Context.Items.TryGetValue("InstanceId", out var existingInstanceId))
        {
            var cancelResponse = await client.GetAsync($"http://localhost:7115/api/CancelChat/${existingInstanceId}");
        }

        Context.Items["InstanceId"] = instanceId;
        await Clients.Caller.SendAsync("Agent", "New chat started! Let's go!");
    }
}