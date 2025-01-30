using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.AI;
using Newtonsoft.Json;

namespace DurableAgentFunctions.ServerlessAgents;

public abstract class LlmAgentEntity : TaskEntity<AgentState>
{
    private readonly IChatClient _chatClient;
    private readonly HubConnection _hubConnection;
    private static readonly Regex MessageRegex = new("^([a-zA-Z0-9].*?)\\|([a-zA-Z0-9].*?)\\|");

    public LlmAgentEntity(IChatClient chatClient, HubConnection hubConnection)
    {
        _chatClient = chatClient;
        _hubConnection = hubConnection;
    }
    
    protected abstract string SystemPrompt { get; }

    public void Init(AgentState state)
    {
        State = state;
    }

    public void AgentHasSpoken(AgentConversationTypes.AgentResponse response)
    {
        if (response.From.Equals("WRITER", StringComparison.InvariantCultureIgnoreCase))
        {
            State.CurrentStory = response.Message;
        }
    }

    public async Task<AgentConversationTypes.AgentResponse> GetResponse(params AgentConversationTypes.AgentResponse[] newMessagesToAgent)
    {
        if (newMessagesToAgent.Any())
        {
            State.ChatHistory = State.ChatHistory.Concat(newMessagesToAgent).ToArray();
        }

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
        await ApplyAgentCustomLogic(agentResponse);
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


    protected virtual Task ApplyAgentCustomLogic(
        AgentConversationTypes.AgentResponse agentResponse) =>
        Task.CompletedTask;

    protected virtual IEnumerable<ChatMessage> BuildChatHistory(IEnumerable<AgentConversationTypes.AgentResponse> history) => 
        history.Select(x => new ChatMessage(x.From.Equals("HUMAN", StringComparison.InvariantCultureIgnoreCase) ? ChatRole.User : ChatRole.Assistant, x.Message));

}