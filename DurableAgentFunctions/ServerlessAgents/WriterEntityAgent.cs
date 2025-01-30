using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents;

public class WriterEntityAgent: LlmAgentEntity
{
    public WriterEntityAgent(IChatClient chatClient, HubConnection hubConnection) : base(chatClient, hubConnection) { }
    
    protected override string SystemPrompt =>
        """
        You are a fabulous writer. 
        You will work with the human and an expert editor, to write a story, taking into account all editor and human Feedback.
        
        Work with what you're given. Don't ask for anymore feedback.
        
        Respond with JSON in the following format: 
        {
            "from": "WRITER",
            "next": "EDITOR",
            "message": "...The story..."
        }
        
        "next" must be either EDITOR.
        
        Use EDITOR when you have a story to review. 
        """;

    [Function(nameof(WriterEntityAgent))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<WriterEntityAgent>();
    }
}
