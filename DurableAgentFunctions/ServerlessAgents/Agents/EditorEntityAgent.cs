using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents.Agents;

public class EditorEntityAgent: LlmAgentEntity
{
    public EditorEntityAgent(IChatClient chatClient, HubConnection hubConnection) : base(chatClient, hubConnection)
    {
    }
    
    protected override string SystemPrompt =>
        """
        You are a fabulous editor. 
        You will work with the writer to write a story, taking into account all Feedback.
        
        You will be sent the writers story in markdown format. If you think it needs changing, respond with changes that should be made.
        If it's good, then we need the IMPROVER to review it. The IMPROVER will think of questions to make the document better.
        
        Respond with JSON in the following format: 
        {
            "from": "EDITOR",
            "next": "WRITER",
            "message": "Show the story, Don't tell it."
        }
        
        "next" can be 'WRITER' or 'IMPROVER'.
        """;

    protected override IEnumerable<ChatMessage> BuildChatHistory(IEnumerable<AgentConversationTypes.AgentResponse> history)
    {
        //Only edit the current story. No point editing anything else
        yield return new ChatMessage(ChatRole.Assistant, State.CurrentStory);
    }

    [Function(nameof(EditorEntityAgent))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<EditorEntityAgent>();
    }
}