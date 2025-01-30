using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents;

public class OrchestratorEntityAgent: LlmAgentEntity
{
    public OrchestratorEntityAgent(IChatClient chatClient, HubConnection hubConnection) : base(chatClient, hubConnection) { }
    
    protected override string SystemPrompt =>
        """
        You are the coordinator who will help the user write a story. 
        You will work with the story writer to write it.
        The human will provide input to the story. You must pass this feedback on to the writer else they won't be aware of it.

        Your job is to coordinate the writing of the story. Based on the conversation, tell us
        who needs to do what next. If the human is happy then respond with 'END'.
        Always send the HUMANS response to the WRITER. You shouldn't alter it unless you really think it's necessary.

        Only ask the user initially for a vague story. It's not your job to make the story better.

        Respond with JSON in the following format: 
        {
            "from": "FACILITATOR",
            "next": "WRITER",
            "message": "Do you like the story, or do you want to make changes?"
        }

        "next" can be 'WRITER', 'HUMAN', or 'END'.
        """;

    [Function(nameof(OrchestratorEntityAgent))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<OrchestratorEntityAgent>();
    }
}