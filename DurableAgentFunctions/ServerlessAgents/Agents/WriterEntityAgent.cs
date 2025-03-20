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

        Each time it's your turn you must either:
            - Ask the RESEARCHER for more information if the HUMAN is referring to things that actually happened recently.
            - Broadcast a new story or an update to the current story using the provided information and feedback. 
        
        You must broadcast all new stories. 
        Then you can ask the IMPROVER to think about questions to make them better.
        
        If the HUMAN's comments suggest you should though, you can throw it away and start again.
        
        The story is finished when the HUMAN tells you explicitly they are happy with it. Until then, write new drafts of the story incorporating the feedback.
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
        responses.Add(new AgentConversationTypes.AgentResponse("STORY", "WRITER", "", story));
        
        //If the improver has spoken then broadcast the message.
        await HubConnection.InvokeAsync(
            "NotifyAgentStoryResponse",
            State.SignalrChatIdentifier,
            story);
        

    }
}
