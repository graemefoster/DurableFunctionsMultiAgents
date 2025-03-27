using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents.Agents;

public class OrchestratorEntityAgent: LlmAgentEntity
{
    public OrchestratorEntityAgent(IChatClient chatClient, HubConnection hubConnection) : base(chatClient, hubConnection) { }
    
    protected override string SystemPrompt =>
        """
        You are the coordinator who will help the user write a story. 
        You will work with other agents who will help you.

        The HUMAN will provide input to the story.

        Your job is to COORDINATE the agents to produce the story. 

        """;

    [Function(nameof(OrchestratorEntityAgent))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<OrchestratorEntityAgent>();
    }
}