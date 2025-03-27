using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents.Planners;

public class NetworkPlanner: IPlanner
{
    private readonly FriendAgent[] _stateAgentsICanTalkTo;

    public NetworkPlanner(FriendAgent[] stateAgentsICanTalkTo)
    {
        _stateAgentsICanTalkTo = stateAgentsICanTalkTo;
    }

    public string GenerateRules(string agentName)
    {
        return $"""
                You must choose ONE agent that is listed below, and send ONE message to it.
                
                Here are your fellow agents:
                
                {string.Join("\n\n", _stateAgentsICanTalkTo.Select(x => $"{x.Name} - {x.Capability}"))}.

                Remember: only send a single message to ONE agent.
                
                """;
    }

    protected virtual string SpecialRules()
    {
        return "";
    }

    public IEnumerable<AITool> GetCustomTools(AgentState agentState, List<AgentConversationTypes.AgentResponse> responseCollector)
    {
        yield return AIFunctionFactory.Create(
            (string nextAgent, string message) => SendMessageToAgent(responseCollector, agentState.AgentName, nextAgent, message),
            new AIFunctionFactoryCreateOptions()
            {
                Name = nameof(SendMessageToAgent),
                Description = "Send a message to another agent"
            });
    }
    
    private void SendMessageToAgent(IList<AgentConversationTypes.AgentResponse> responses, string agentName, string nextAgent, string message)
    {
        responses.Add(
            new AgentConversationTypes.AgentResponse(
                "MESSAGE", 
                DateTimeOffset.Now, 
                agentName, 
                nextAgent.ToUpperInvariant(),
                message));
    }

}