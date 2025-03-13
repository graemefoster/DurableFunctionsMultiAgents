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
            "message": "<something that makes sense to ask of this agent>."
        }
    ]
}

You can talk to as many other agents as you need to.
The available agents are:
{{string.Join($"{Environment.NewLine}", State.AgentsICanTalkTo.Select(x => $"{x.Name} - {x.Capability}"))}}.

The messages must be something that makes sense to ask the agents.

Remember - the output must be the shown JSON object.

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
        
        return agentRequests;
    }
    
    
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

    protected virtual IEnumerable<ChatMessage> BuildChatHistory(IEnumerable<AgentConversationTypes.AgentResponse> history) => 
        history.Select(x => new ChatMessage(x.From.Equals("HUMAN", StringComparison.InvariantCultureIgnoreCase) ? ChatRole.User : ChatRole.Assistant, x.Message));

}