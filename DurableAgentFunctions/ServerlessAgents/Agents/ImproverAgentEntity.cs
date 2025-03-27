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
        """;

    [Function(nameof(ImproverAgentEntity))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<ImproverAgentEntity>();
    }

}