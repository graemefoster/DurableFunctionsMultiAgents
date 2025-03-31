using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace SemanticKernelWriterMultiAgent.Agents;

public static class WriterEntityAgent
{
    public static ChatCompletionAgent Build(Kernel kernel)
    {
        return new ChatCompletionAgent()
        {
            Name = "WRITER",
            Instructions =
                """
                You are a fabulous writer. You will work with a team to write a story.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new AzureOpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
        };

        // protected override IEnumerable<ChatMessage> BuildChatHistory(
        //     IEnumerable<AgentConversationTypes.AgentResponse> history)
        // {
        //     yield return new ChatMessage(ChatRole.Assistant, "Current Story Follows:");
        //     yield return new ChatMessage(ChatRole.Assistant, base.State.CurrentStory);
        //     
        //     //Diffs come from the human so we will always have a human message
        //     yield return new ChatMessage(ChatRole.User, history.Last(x => x.From == "HUMAN").Message);
        // }
        //
        // [Function(nameof(DifferEntityAgent))]
        // public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
        // {
        //     return dispatcher.DispatchAsync<DifferEntityAgent>();
        // }
        //
        // private void NoChange(IList<AgentConversationTypes.AgentResponse> responses)
        // {
        //     responses.Add(new AgentConversationTypes.AgentResponse("MESSAGE", DateTimeOffset.Now, "DIFFER", "HUMAN", "No significant changes were found"));
        // }
        //
        // private void Changes(IList<AgentConversationTypes.AgentResponse> responses, string summary)
        // {
        //     responses.Add(new AgentConversationTypes.AgentResponse("MESSAGE", DateTimeOffset.Now, "DIFFER", "WRITER", summary));
        // }
        //
        // protected override IEnumerable<AITool> GetCustomTools(IList<AgentConversationTypes.AgentResponse> responses)
        // {
        //     yield return AIFunctionFactory.Create(() => NoChange(responses), new AIFunctionFactoryCreateOptions()
        //     {
        //         Description = "When no significant changes were found",
        //     });
        //     yield return AIFunctionFactory.Create((string changeSummary) => Changes(responses, changeSummary), new AIFunctionFactoryCreateOptions()
        //     {
        //         Description = "Summarise the intent of the changes made by the HUMAN"
        //     });
        // }
    }
}
