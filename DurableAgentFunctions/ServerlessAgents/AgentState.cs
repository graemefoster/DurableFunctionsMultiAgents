namespace DurableAgentFunctions.ServerlessAgents;

public class AgentState
{
    public required string SignalrChatIdentifier { get; set; } 
    public AgentResponse[] ChatHistory { get; set; } = [];
}

