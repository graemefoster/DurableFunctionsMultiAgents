using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents.Agents;

public class DifferEntityAgent: LlmAgentEntity
{
    public DifferEntityAgent(IChatClient chatClient, HubConnection hubHubConnection) : base(chatClient, hubHubConnection)
    {
    }

    protected override string SystemPrompt =>
        """
        You are a great analyser of content and you have a remarkable ability to look at a git diff of text, and summarise 
        what the user was trying to change.
        """;

    protected override IEnumerable<ChatMessage> BuildChatHistory(
        IEnumerable<AgentConversationTypes.AgentResponse> history)
    {
        yield return new ChatMessage(ChatRole.Assistant, "Current Story Follows:");
        yield return new ChatMessage(ChatRole.Assistant, base.State.CurrentStory);
        
        //Diffs come from the human so we will always have a human message
        yield return new ChatMessage(ChatRole.User, history.Last(x => x.From == "HUMAN").Message);
    }

    [Function(nameof(DifferEntityAgent))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<DifferEntityAgent>();
    }

    private void NoChange(IList<AgentConversationTypes.AgentResponse> responses)
    {
        responses.Add(new AgentConversationTypes.AgentResponse("MESSAGE", DateTimeOffset.Now, "DIFFER", "HUMAN", "No significant changes were found"));
    }

    private void Changes(IList<AgentConversationTypes.AgentResponse> responses, string summary)
    {
        responses.Add(new AgentConversationTypes.AgentResponse("MESSAGE", DateTimeOffset.Now, "DIFFER", "WRITER", summary));
    }

    protected override IEnumerable<AITool> GetCustomTools(IList<AgentConversationTypes.AgentResponse> responses)
    {
        yield return AIFunctionFactory.Create(() => NoChange(responses), new AIFunctionFactoryCreateOptions()
        {
            Description = "When no significant changes were found",
        });
        yield return AIFunctionFactory.Create((string changeSummary) => Changes(responses, changeSummary), new AIFunctionFactoryCreateOptions()
        {
            Description = "Summarise the intent of the changes made by the HUMAN"
        });
    }
}