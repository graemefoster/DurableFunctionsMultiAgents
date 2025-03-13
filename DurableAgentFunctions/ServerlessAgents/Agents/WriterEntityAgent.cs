using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents.Agents;

public class WriterEntityAgent: LlmAgentEntity
{
    public WriterEntityAgent(IChatClient chatClient, HubConnection hubConnection) : base(chatClient, hubConnection) { }

    protected override string SystemPrompt =>
        """
        You are a fabulous writer. You will work with a team to write a story.

        Each time it's your turn you must either:
            - Write a new story if there is no current one, or update the current story using the provided feedback.
            - Ask the RESEARCHER if you need up to date information sourced from the Internet.
        
        If the HUMAN's comments suggest you should though, you can throw it away and start again.
        """;
 
    [Function(nameof(WriterEntityAgent))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<WriterEntityAgent>();
    }

    protected override IEnumerable<ChatMessage> BuildChatHistory(IEnumerable<AgentConversationTypes.AgentResponse> history)
    {
        var newHistory = base.BuildChatHistory(history).ToList();

        //Make sure the current story is always at the top
        if (State.CurrentStory != null)
        {
            newHistory.Insert(0, new ChatMessage(ChatRole.Assistant, base.State.CurrentStory));
            newHistory.Insert(0, new ChatMessage(ChatRole.Assistant, "Current Story Follows:"));
        }
        else
        {
            newHistory.Insert(0, new ChatMessage(ChatRole.Assistant, "There is NO current story"));
        }

        return newHistory;
    }
}
