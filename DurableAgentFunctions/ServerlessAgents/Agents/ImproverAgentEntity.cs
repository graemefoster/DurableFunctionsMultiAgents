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
        Have a look at a document that's been written and edited, and try to think of follow up questions that will make it better.

        Respond with JSON in the following format: 
        {
            "from": "IMPROVER",
            "next": "HUMAN",
            "message": "<thought provoking questions to improve the story>."
        }

        "next" has to be "HUMAN". 
        The message should be one or more questions that will provoke the human to make the story better.
        """;

    protected override IEnumerable<ChatMessage> BuildChatHistory(
        IEnumerable<AgentConversationTypes.AgentResponse> history)
    {
        //Only improve on the current story. Don't bother with anything else
        yield return new ChatMessage(ChatRole.Assistant, State.CurrentStory!);
    }

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