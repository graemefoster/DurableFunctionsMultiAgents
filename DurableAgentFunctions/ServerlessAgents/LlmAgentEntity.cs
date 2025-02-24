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
    
    protected override async Task<AgentConversationTypes.AgentResponse> GetResponseInternal(AgentConversationTypes.AgentResponse newMessageToAgent)
    {
        State.ChatHistory = State.ChatHistory.Concat([newMessageToAgent]).ToArray();

        var messages = new[]
            {
                new ChatMessage(ChatRole.System, SystemPrompt)
            }
            .Concat(BuildChatHistory(State.ChatHistory))
            .ToArray();

        var response = await _chatClient.CompleteAsync(
            messages.ToList(),
            new ChatOptions()
            {
                ResponseFormat = ChatResponseFormat.Json
            });
        
        var agentResponse = JsonConvert.DeserializeObject<AgentConversationTypes.AgentResponse>(response.Message.Text!)!;
        agentResponse = await ApplyAgentCustomLogic(agentResponse);

        await BroadcastPrompt(messages, agentResponse);
        
        return agentResponse;
    }
    
    
    private async Task BroadcastPrompt(ChatMessage[] messages, AgentConversationTypes.AgentResponse response)
    {
        await _hubConnection.InvokeAsync("BroadcastPrompt", 
            State.SignalrChatIdentifier, 
            State.AgentName, 
            messages
                .Select(x => $"{x.Role.Value}: {x.Text}")
                .Union([$"LLM RESPONSE -> {response.Next}: {response.Message}"])
                .ToArray());
    }

    protected virtual Task<AgentConversationTypes.AgentResponse> ApplyAgentCustomLogic(
        AgentConversationTypes.AgentResponse agentResponse) => Task.FromResult(agentResponse);

    protected virtual IEnumerable<ChatMessage> BuildChatHistory(IEnumerable<AgentConversationTypes.AgentResponse> history) => 
        history.Select(x => new ChatMessage(x.From.Equals("HUMAN", StringComparison.InvariantCultureIgnoreCase) ? ChatRole.User : ChatRole.Assistant, x.Message));

}