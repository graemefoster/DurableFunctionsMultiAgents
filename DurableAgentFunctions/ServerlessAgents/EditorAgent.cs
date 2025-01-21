using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DurableAgentFunctions.ServerlessAgents;

public class EditorAgent: LlmAgent
{
    public EditorAgent(IChatClient chatClient): base(chatClient)
    {
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
    
    protected override async Task ApplyAgentCustomLogic(FunctionContext context, PostProcessAgentResponse response)
    {
        if (response.Response.Next.Equals("HUMAN", StringComparison.InvariantCultureIgnoreCase))
        {
            var story = response.ChatHistory.Last(x =>
                x.From.Equals("WRITER", StringComparison.InvariantCultureIgnoreCase));
            
            await NotifyUserStoryUpdate(
                new AgentUpdateToHuman(response.SignalrChatIdentifier, story.Message),
                context);
        }
    }

    /// <summary>
    /// Only want the latest draft of the story
    /// </summary>
    protected override IEnumerable<ChatMessage> BuildChatHistory(IEnumerable<AgentResponse> history)
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

    public async Task NotifyUserStoryUpdate(
        [ActivityTrigger] AgentUpdateToHuman result,
        FunctionContext executionContext)
    {
        var signalrHub = executionContext.InstanceServices.GetRequiredService<HubConnection>();
        var logger = executionContext.GetLogger(nameof(NotifyUserStoryUpdate));

        logger.LogInformation(
            "Notifying agent of result: {result} from connection id: {connectionId}", result.Result,
            result.SignalrChatIdentifier);

        await signalrHub.InvokeAsync("NotifyAgentStoryResponse", result.SignalrChatIdentifier, result.Result);
    }
}