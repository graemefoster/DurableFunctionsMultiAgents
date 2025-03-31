using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace SemanticKernelWriterMultiAgent.Agents;

public static class ImproverEntityAgent
{
    public static ChatCompletionAgent Build(Kernel kernel)
    {
        return new ChatCompletionAgent()
        {
            Name = "IMPROVER",
            Instructions =
                """
                You are amazing at analyzing a document and coming up with follow up questions..
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new AzureOpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
        };
    }
}
