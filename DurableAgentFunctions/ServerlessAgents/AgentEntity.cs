using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.DurableTask.Entities;

namespace DurableAgentFunctions.ServerlessAgents;

public abstract class AgentEntity : TaskEntity<AgentState>
{
    private readonly HubConnection _hubConnection;

    public AgentEntity(HubConnection hubConnection)
    {
        _hubConnection = hubConnection;
    }

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

    public async Task<AgentConversationTypes.AgentResponse[]> GetResponse(AgentConversationTypes.AgentResponse newMessageToAgent)
    {
        var responses = await GetResponseInternal(newMessageToAgent);
        foreach(var response in responses) await BroadcastInternalChitChat(response);
        return responses;
    }

    protected abstract Task<AgentConversationTypes.AgentResponse[]> GetResponseInternal(AgentConversationTypes.AgentResponse newMessageToAgent);

    /// <summary>
    /// Shows all the interactive chit-chat between agents
    /// </summary>
    private async Task BroadcastInternalChitChat(
        AgentConversationTypes.AgentResponse response)
    {
        if (response.Next != "HUMAN")
        {
            await _hubConnection.InvokeAsync(
                "AgentChitChat",
                State.SignalrChatIdentifier,
                response.From,
                response.Next,
                response.Message);
        }
    }
}