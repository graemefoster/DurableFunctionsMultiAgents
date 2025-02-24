using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents.Agents;

public class WriterEntityAgent: LlmAgentEntity
{
    public WriterEntityAgent(IChatClient chatClient, HubConnection hubConnection) : base(chatClient, hubConnection) { }
    
    protected override string SystemPrompt =>
        """
        You are a fabulous writer.
        You will work with the human and an expert editor, to write a story, taking into account all editor and human Feedback.
        
        Work with what you're given. Don't ask for anymore feedback. If you previously wrote a story you will be supplied it.
        Don't completely rewrite it - just update it given the feedback. Unless the HUMAN's comments suggest you should throw it away and start again.
        
        Respond with JSON in the following format: 
        {
            "from": "WRITER",
            "next": "EDITOR",
            "message": "...The story..."
        }
        
        "next" must be EDITOR or RESEARCHER. 
        
        Use EDITOR to check a story you've written for grammar. Use RESEARCHER if you need information from the internet to help you write the story.
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
