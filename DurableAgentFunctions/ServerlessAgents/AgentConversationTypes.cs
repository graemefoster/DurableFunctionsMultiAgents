namespace DurableAgentFunctions.ServerlessAgents;

public static class AgentConversationTypes
{
    public record AgentQuestionToHuman(string EventName, AgentResponse Question);
    public record AgentResponse(string From, string Next, string Message);
}
