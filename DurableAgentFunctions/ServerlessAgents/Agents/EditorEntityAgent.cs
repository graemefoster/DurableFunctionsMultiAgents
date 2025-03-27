using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents.Agents;

public class EditorEntityAgent: LlmAgentEntity
{
    public EditorEntityAgent(IChatClient chatClient, HubConnection hubHubConnection) : base(chatClient, hubHubConnection)
    {
    }

    protected override string SystemPrompt =>
        """
        You are an EDITOR who specialises in grammar and punctuation. 
        """;

    protected override IEnumerable<ChatMessage> BuildChatHistory(IEnumerable<AgentConversationTypes.AgentResponse> history)
    {
        //Only edit the current story. No point editing anything else
        yield return new ChatMessage(ChatRole.Assistant, "Current Story Follows:");
        yield return new ChatMessage(ChatRole.Assistant, base.State.CurrentStory);
    }

    [Function(nameof(EditorEntityAgent))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<EditorEntityAgent>();
    }
}