using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Entities;

namespace DurableAgentFunctions.ServerlessAgents.Agents;

public class EndAgentEntity : TaskEntity<AgentState>
{
    public void Init(AgentState state)
    {
        State = state;
    }

    public void AgentHasSpoken(AgentConversationTypes.AgentResponse response) { }

    [Function(nameof(EndAgentEntity))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<EndAgentEntity>();
    }
    
}