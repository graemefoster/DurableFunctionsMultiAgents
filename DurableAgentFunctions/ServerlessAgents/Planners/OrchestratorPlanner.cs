using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents.Planners;

public class OrchestratorPlanner: IPlanner
{
    private readonly FriendAgent[] _stateAgentsICanTalkTo;

    public OrchestratorPlanner(FriendAgent[] stateAgentsICanTalkTo)
    {
        _stateAgentsICanTalkTo = stateAgentsICanTalkTo;
    }

    public string GenerateRules(string agentName)
    {
        if (agentName != "ORCHESTRATOR")
        {
            return """
                    You MUST send a message TO the ORCHESTRATOR agent with your response. The ORCHESTRATOR will decide what to do next.
                    """;
        }
        
        return $"""
                You must choose ONE agent that is listed below, and send ONE message to it.

                CHOOSE from ONE of these agents:
                {string.Join("\n", _stateAgentsICanTalkTo.Select(x => $"{x.Name} - {x.Capability}"))}.

                Remember: only send a single message to ONE agent.

                """;

    }

    public IEnumerable<AITool> GetCustomTools(AgentState agentState, List<AgentConversationTypes.AgentResponse> responseCollector)
    {
        if (agentState.AgentName == "ORCHESTRATOR")
        {
            yield return AIFunctionFactory.Create(
                (string nextAgent, string message) =>
                    SendMessageToAgent(responseCollector, agentState.AgentName, nextAgent, message),
                new AIFunctionFactoryCreateOptions
                {
                    Name = nameof(SendMessageToAgent),
                    Description = "Send a message to another agent"
                });
        }
        else
        {
            yield return AIFunctionFactory.Create(
                (string message) =>
                    SendMessageToAgent(responseCollector, agentState.AgentName, "ORCHESTRATOR", message),
                new AIFunctionFactoryCreateOptions
                {
                    Name = "SendMessageToOrchestrator",
                    Description = "Send a message to the ORCHESTRATOR"
                });
        }

    }

    public AgentConversationTypes.AgentResponse GetNextGuessAgent(string agent, string message)
    {
        return new AgentConversationTypes.AgentResponse("MESSAGE", DateTimeOffset.Now, agent, "ORCHESTRATOR", message);
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