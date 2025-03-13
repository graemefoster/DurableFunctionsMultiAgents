namespace DurableAgentFunctions.ServerlessAgents;

public class AgentState
{
    public required string SignalrChatIdentifier { get; set; } 
    public AgentConversationTypes.AgentResponse[] ChatHistory { get; set; } = [];
    public required string AgentName { get; set; }
    public string? CurrentStory { get; set; }

    public required FriendAgent[] AgentsICanTalkTo { get; set; }

    public required string AgentSummary { get; set; }
}

public class FriendAgent
{
    public required string Name { get; set; }
    public required string Capability { get; set; }
}

