using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Entities;

namespace DurableAgentFunctions.ServerlessAgents.Agents;

public class UserAgentEntity : TaskEntity<AgentState>
{
    private readonly HubConnection _hubConnection;

    public UserAgentEntity(HubConnection hubConnection)
    {
        _hubConnection = hubConnection;
    }

    public void Init(AgentState state)
    {
        State = state;
    }

    public void AgentHasSpoken(AgentConversationTypes.AgentResponse response) { }

    public async Task AskQuestion(AgentConversationTypes.AgentQuestionToHuman newMessagesToAgent)
    {
        State.ChatHistory = State.ChatHistory.Concat([newMessagesToAgent.Question]).ToArray();

        await _hubConnection.InvokeAsync(
            "AskForUserInput",
            State.SignalrChatIdentifier,
            newMessagesToAgent.Question.From,
            newMessagesToAgent.Question.Message,
            newMessagesToAgent.EventName);
    }
    
    public AgentConversationTypes.AgentResponse RecordResponse(FunctionPayloads.HumanResponseToAgentQuestion response)
    {
        var agentResponse = new AgentConversationTypes.AgentResponse("MESSAGE", "HUMAN", response.NextAgent, response.Response);
        State.ChatHistory = State.ChatHistory.Concat([agentResponse]).ToArray();
        return agentResponse;
    }


    [Function(nameof(UserAgentEntity))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<UserAgentEntity>();
    }
    
}