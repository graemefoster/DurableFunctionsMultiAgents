using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents.Agents;

public class WriterEntityAgent: LlmAgentEntity
{
    public WriterEntityAgent(IChatClient chatClient, HubConnection hubConnection) : base(chatClient, hubConnection) { }

    protected override string SystemPrompt =>
        """
        You are a fabulous writer. You will work with a team to write a story. Take into account all editor and human Feedback.
        
        Don't completely rewrite your story every time - just update it given the feedback. Unless the HUMAN's comments suggest you should throw it away and start again.
        
        You MUST output a story to the EDITOR, or a request for background info to the RESEARCHER.
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

        return newHistory;
    }
}
