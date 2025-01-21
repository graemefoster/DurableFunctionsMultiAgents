using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents;

public class EditorEntityAgent: LlmAgentEntity
{
    private readonly HubConnection _hubConnection;
    public EditorEntityAgent(IChatClient chatClient, HubConnection hubConnection) : base(chatClient)
    {
        _hubConnection = hubConnection;
    }
    
    protected override string SystemPrompt =>
        """
        You are a fabulous editor. 
        You will work with the writer to write a story, taking into account all Feedback.
        
        You will be sent the writers story in markdown format. If you think it needs changing, respond with changes that should be made.
        If it's good, then we need the HUMAN to review it, so the next step is to target the 'HUMAN'.
        
        Respond with JSON in the following format: 
        {
            "from": "EDITOR",
            "next": "WRITER",
            "message": "Show the story, Don't tell it."
        }
        
        "next" can be 'WRITER' or 'HUMAN'.
        """;

    protected override IEnumerable<ChatMessage> BuildChatHistory(IEnumerable<AgentConversationTypes.AgentResponse> history)
    {
        var storyDraft = history.Last(x => x.From.Equals("WRITER", StringComparison.InvariantCultureIgnoreCase));
        yield return new ChatMessage(ChatRole.Assistant, storyDraft.Message);

        foreach (var agentResponse in history)
        {
            if (!agentResponse.From.Equals("WRITER", StringComparison.InvariantCultureIgnoreCase))
            {
                yield return new ChatMessage(
                    agentResponse.From.Equals("HUMAN", StringComparison.InvariantCultureIgnoreCase)
                        ? ChatRole.User
                        : ChatRole.Assistant, agentResponse.Message);
            }
        }
    }

    protected override async Task ApplyAgentCustomLogic(AgentConversationTypes.AgentResponse agentResponse)
    {
        if (agentResponse.Next.Equals("HUMAN", StringComparison.InvariantCultureIgnoreCase))
        {
            var story = State.ChatHistory.Last(x =>
                x.From.Equals("WRITER", StringComparison.InvariantCultureIgnoreCase));

            await _hubConnection.InvokeAsync(
                "NotifyAgentStoryResponse",
                State.SignalrChatIdentifier,
                story.Message);
        }

    }

    [Function(nameof(EditorEntityAgent))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<EditorEntityAgent>();
    }
}