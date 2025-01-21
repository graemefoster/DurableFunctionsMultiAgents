using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents;

public class OrchestratorEntityAgent: LlmAgentEntity
{
    public OrchestratorEntityAgent(IChatClient chatClient) : base(chatClient) { }
    
    protected override string SystemPrompt =>
        """
        You are the coordinator who will help the user write a story. 
        You will work with the story writer to write it.
        The human will provide input to the story.

        Your job is to coordinate the writing of the story. Based on the conversation, tell us
        who needs to do what next. If the human is happy then respond with 'END'.

        If you don't know what the user wants to write a story about, you must ask them.

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