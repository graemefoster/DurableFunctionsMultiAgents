namespace DurableAgentFunctions.ServerlessAgents;

public class AgentState
{
    public required string SignalrChatIdentifier { get; set; } 
    public AgentConversationTypes.AgentResponse[] ChatHistory { get; set; } = [];
}

