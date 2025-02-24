using DurableAgentFunctions.ServerlessAgents;

namespace DurableAgentFunctions;

public static class FunctionPayloads
{
    public record BeginChatToHuman(string SignalrChatIdentifier);
    public record HumanResponseToAgentQuestion(string InstanceId, string EventName, string Response, string NextAgent);
    public record AgentChitChat(string SignalrChatIdentifier, AgentConversationTypes.AgentResponse Response);
    
}
