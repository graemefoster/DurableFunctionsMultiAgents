using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents.Agents;

public class WriterEntityAgent: LlmAgentEntity
{
    public WriterEntityAgent(IChatClient chatClient, HubConnection hubConnection) : base(chatClient, hubConnection) { }

    protected override string SystemPrompt =>
        """
        You are a fabulous writer. You will work with a team to write a story.

        Each time it's your turn you must either:
            - Ask the RESEARCHER for more information if the HUMAN is referring to things that actually happened recently.
            - Write a new story if there is no current one, or update the current story using the provided feedback.
        
        If the HUMAN's comments suggest you should though, you can throw it away and start again.
        
        Just output the story. The next agent will know what they need to do!
        """;
 
    [Function(nameof(WriterEntityAgent))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<WriterEntityAgent>();
    }

    protected override bool ShouldRetainInHistory(AgentConversationTypes.AgentResponse agentRequest)
    {
        //don't retain every story draft in history
        return agentRequest.Next != "IMPROVER";
    }
}
