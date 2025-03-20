using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents.Agents;

public class ImproverAgentEntity : LlmAgentEntity
{
    public ImproverAgentEntity(IChatClient chatClient, HubConnection hubHubConnection) : base(chatClient, hubHubConnection)
    {
    }

    protected override string SystemPrompt =>
        """
        You are amazing at analyzing a document and coming up with follow up questions.

        Each time it's your turn:
        - Try to think of follow up questions to ask the HUMAN to make the story better. Just ask questions. No need to tell them the story as they already have a copy.
        - Or relay the HUMAN's feedback to the WRITER so the WRITER can improve the story.
        
        """;

    [Function(nameof(ImproverAgentEntity))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<ImproverAgentEntity>();
    }

}