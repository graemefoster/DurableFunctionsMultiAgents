using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents.Agents;

public class DifferEntityAgent: LlmAgentEntity
{
    public DifferEntityAgent(IChatClient chatClient, HubConnection hubConnection) : base(chatClient, hubConnection)
    {
    }

    protected override string SystemPrompt =>
        """
        You are a great analyser of content and you have a remarkable ability to look at a git diff of text, and summarise 
        what the user was trying to change.
        
        If the user has really just replaced the original content then response with a message that says "NEW_CONTENT".
        If the only changes are to whitespace, then respond with a message that says "NO_CHANGE".
        Else respond with a message that captures the intent of the change.
        """;

    protected override IEnumerable<ChatMessage> BuildChatHistory(
        IEnumerable<AgentConversationTypes.AgentResponse> history)
    {
        yield return new ChatMessage(ChatRole.Assistant, "Current Story Follows:");
        yield return new ChatMessage(ChatRole.Assistant, base.State.CurrentStory);
        
        //Diffs come from the human so we will always have a human message
        yield return new ChatMessage(ChatRole.User, history.Last(x => x.From == "HUMAN").Message);
    }

    protected override Task<AgentConversationTypes.AgentResponse> ApplyAgentCustomLogic(AgentConversationTypes.AgentResponse agentResponse)
    {
        if (agentResponse.Message == "NO_CHANGE")
        {
            return Task.FromResult(
                new AgentConversationTypes.AgentResponse("DIFFER", "HUMAN", "No significant changes were found"));
        }
        return base.ApplyAgentCustomLogic(agentResponse);
    }

    [Function(nameof(DifferEntityAgent))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<DifferEntityAgent>();
    }
}