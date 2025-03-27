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
                **RULES**
                ---------
                You can only send a message to ONE agent and they MUST be listed below. IF YOU TRY TO TALK TO MORE, WE WILL ONLY USE THE FIRST ONE.

                The available agents are:
                {string.Join("\n\n", _stateAgentsICanTalkTo.Select(x => $"{x.Name} - {x.Capability}"))}.

                Remember you cannot talk DIRECTLY to any other agent than the listed ones.

                """;
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