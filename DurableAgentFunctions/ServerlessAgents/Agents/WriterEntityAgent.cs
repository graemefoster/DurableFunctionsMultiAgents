using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents.Agents;

public class WriterEntityAgent: LlmAgentEntity
{
    public WriterEntityAgent(IChatClient chatClient, HubConnection hubHubConnection) : base(chatClient, hubHubConnection) { }

    protected override string SystemPrompt =>
        """
        You are a fabulous writer. You will work with a team to write a story.

        Whenever you write a new version of the story you must BROADCAST it, then send messages to others.
        """;
 
    [Function(nameof(WriterEntityAgent))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<WriterEntityAgent>();
    }

    protected override IEnumerable<AITool> GetCustomTools(IList<AgentConversationTypes.AgentResponse> responses)
    {
        yield return AIFunctionFactory.Create(
            (string story) => BroadcastStory(responses, story),
            new AIFunctionFactoryCreateOptions()
            {
                Name = nameof(BroadcastStory),
                Description = "Use this to broadcast a story when you've written or updated one"
            });
    }

    private async ValueTask BroadcastStory(IList<AgentConversationTypes.AgentResponse> responses, string story)
    {
        responses.Add(new AgentConversationTypes.AgentResponse("STORY", DateTimeOffset.Now, "WRITER", "", story));
        
        //If the improver has spoken then broadcast the message.
        await HubConnection.InvokeAsync(
            "NotifyAgentStoryResponse",
            State.SignalrChatIdentifier,
            story);
        

    }
}
