using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents.Planners;

public interface IPlanner
{
    string GenerateRules(string agentName);
    IEnumerable<AITool> GetCustomTools(AgentState state, List<AgentConversationTypes.AgentResponse> allResponses);
    AgentConversationTypes.AgentResponse GetNextGuessAgent(string agent, string message);
}
