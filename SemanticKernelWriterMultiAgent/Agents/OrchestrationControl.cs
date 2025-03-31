using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace SemanticKernelWriterMultiAgent.Agents;

public static class OrchestrationControl
{
    [Experimental("SKEXP0110")]
    public static KernelFunction BuildNextAgentFunction(Dictionary<string, string> agents)
    {
        return AgentGroupChat.CreatePromptFunctionForStrategy(
            $$$$"""
                Examine the provided RESPONSE and choose the next participant.
                State only the name of the chosen participant without explanation.
                Never choose the participant named in the RESPONSE.

                Choose only from these participants:

                {{{{string.Join("\n\n", agents.Select(x => $"{x.Key} - {x.Value}"))}}}}.
                    
                RESPONSE:
                {{$lastmessage}}

                """, safeParameterNames: "lastmessage");

    }
    
    [Experimental("SKEXP0110")]
    public static KernelFunction BuildTerminationCheckFunction(Dictionary<string, string> agents, string terminationToken = "yes")
    {
        return
            AgentGroupChat.CreatePromptFunctionForStrategy(
                $$$"""
                   Examine the RESPONSE and determine whether the content has been deemed satisfactory.
                   If content is satisfactory, respond with a single word without explanation: {{{terminationToken}}}.
                   If specific suggestions are being provided, it is not satisfactory.
                   If no correction is suggested, it is satisfactory.

                   RESPONSE:
                   {{$lastmessage}}
                   """,
                safeParameterNames: "lastmessage");

    }

}