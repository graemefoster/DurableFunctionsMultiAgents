using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents;

public class ImproverAgentEntity: LlmAgentEntity
{
    private readonly HubConnection _hubConnection;
    public ImproverAgentEntity(IChatClient chatClient, HubConnection hubConnection) : base(chatClient, hubConnection)
    {
        _hubConnection = hubConnection;
    }
    
    protected override string SystemPrompt =>
        """
        You are amazing at analyzing a document and coming up with follow up questions.
        Have a look at a document that's been written and edited, and try to think of follow up questions that will make it better.
        
        Respond with JSON in the following format: 
        {
            "from": "IMPROVER",
            "next": "HUMAN",
            "message": "Some thought provoking questions to improve the story."
        }
        
        "next" has to be "HUMAN". The message should be a question that will provoke the human to provide information to make the story better.
        """;

    protected override IEnumerable<ChatMessage> BuildChatHistory(IEnumerable<AgentConversationTypes.AgentResponse> history)
    {
        return base.BuildChatHistory(history)
            .Concat([new ChatMessage(ChatRole.Assistant, State.CurrentStory!)]);
    }

    protected override async Task ApplyAgentCustomLogic(AgentConversationTypes.AgentResponse agentResponse)
    {

    }

    [Function(nameof(ImproverAgentEntity))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<ImproverAgentEntity>();
    }
}
