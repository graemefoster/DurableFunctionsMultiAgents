using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents.Agents;

public class ImproverAgentEntity : LlmAgentEntity
{
    private readonly HubConnection _hubConnection;

    public ImproverAgentEntity(IChatClient chatClient, HubConnection hubConnection) : base(chatClient, hubConnection)
    {
        _hubConnection = hubConnection;
    }

    protected override string SystemPrompt =>
        """
        You are amazing at analyzing a document and coming up with follow up questions.

        Each time it's your turn:
        - Try to think of follow up questions to ask the HUMAN to make the story better.
        - Or relay the HUMAN's feedback to the WRITER so the WRITER can improve the story.
        
        """;

    protected override async Task<AgentConversationTypes.AgentResponse> ApplyAgentCustomLogic(AgentConversationTypes.AgentResponse agentResponse)
    {
        //If the improver has spoken then broadcast the message.
        await _hubConnection.InvokeAsync(
            "NotifyAgentStoryResponse",
            State.SignalrChatIdentifier,
            State.CurrentStory);

        return agentResponse;
    }

    [Function(nameof(ImproverAgentEntity))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<ImproverAgentEntity>();
    }
}