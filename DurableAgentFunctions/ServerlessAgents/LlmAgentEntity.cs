using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.AI;
using Newtonsoft.Json;

namespace DurableAgentFunctions.ServerlessAgents;

public abstract class LlmAgentEntity : AgentEntity
{
    private readonly IChatClient _chatClient;
    private readonly HubConnection _hubConnection;

    public LlmAgentEntity(IChatClient chatClient, HubConnection hubConnection): base(hubConnection)
    {
        _chatClient = chatClient;
        _hubConnection = hubConnection;
    }
    
    protected abstract string SystemPrompt { get; }
    
    protected override async Task<AgentConversationTypes.AgentResponse[]> GetResponseInternal(AgentConversationTypes.AgentResponse newMessageToAgent)
    {
        State.ChatHistory = State.ChatHistory.Concat([newMessageToAgent]).ToArray();

        var messages = new[]
            {
                new ChatMessage(ChatRole.System, SystemPrompt + 
                    $$""""

You MUST Respond with JSON object containing requests to other agents: It must be in this format:
{
    requests: [
        {
            "from": "{{State.AgentName}}",
            "next": "{{State.AgentsICanTalkTo.First().Name}}",
            "message": "<output to send to the next agent>"
        }
    ]
}
    
You can only talk to ONE agent. IF YOU TRY TO TALK TO MORE, WE WILL ONLY USE THE FIRST ONE.

The available agents are:
{{string.Join($"{Environment.NewLine}", State.AgentsICanTalkTo.Select(x => $"{x.Name} - {x.Capability}"))}}.

Remember - the output must be in the format of the above JSON object.

"""")
            }
            .Concat(BuildChatHistory(State.ChatHistory))
            .ToArray();

        var response = await _chatClient.CompleteAsync(
            messages.ToList(),
            new ChatOptions()
            {
                ResponseFormat = ChatResponseFormat.Json
            });
        
        var agentResponse = JsonConvert.DeserializeObject<AgentConversationTypes.AgentResponses>(response.Message.Text!)!;
        var agentRequests = agentResponse.Requests;
         agentRequests = await Task.WhenAll(agentRequests.Select(ApplyAgentCustomLogic));

        await BroadcastPrompt(messages, agentRequests);

        foreach (var agentRequest in agentRequests)
        {
            if (ShouldRetainInHistory(agentRequest))
            {
                State.ChatHistory = State.ChatHistory.Concat([agentRequest]).ToArray();
            }
        }

        return agentRequests;
    }

    protected virtual bool ShouldRetainInHistory(AgentConversationTypes.AgentResponse agentRequest) => true;


    private async Task BroadcastPrompt(ChatMessage[] messages, AgentConversationTypes.AgentResponse[] responses)
    {
        foreach (var response in responses)
        {
            await _hubConnection.InvokeAsync("BroadcastPrompt",
                State.SignalrChatIdentifier,
                State.AgentName,
                messages
                    .Select(x => $"{x.Role.Value}: {x.Text}")
                    .Union([$"LLM RESPONSE -> {response.Next}: {response.Message}"])
                    .ToArray());
        }
    }

    protected virtual Task<AgentConversationTypes.AgentResponse> ApplyAgentCustomLogic(
        AgentConversationTypes.AgentResponse agentResponse) => Task.FromResult(agentResponse);

    protected virtual IEnumerable<ChatMessage> BuildChatHistory(
        IEnumerable<AgentConversationTypes.AgentResponse> history)
    {
        foreach (var message in history)
        {
            if (!(message is { From: "WRITER", Next: "IMPROVER" }))
            {
                yield return new ChatMessage(
                    message.From.Equals("HUMAN", StringComparison.InvariantCultureIgnoreCase)
                        ? ChatRole.User
                        : ChatRole.Assistant,
                    $"From:{message.From} to {message.Next} - {message.Message}");
            }
        }
        
        //Only improve on the current story. Don't bother with anything else
        if (State.CurrentStory != null)
        {
            yield return  new ChatMessage(ChatRole.Assistant, "Latest Story Follows:");
            yield return new ChatMessage(ChatRole.Assistant, base.State.CurrentStory);
        }
        else
        {
            yield return new ChatMessage(ChatRole.Assistant, "There is NO current story");
        }
    }
}