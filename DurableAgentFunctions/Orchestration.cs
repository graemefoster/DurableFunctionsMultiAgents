using DurableAgentFunctions.ServerlessAgents;
using DurableAgentFunctions.ServerlessAgents.Agents;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DurableAgentFunctions;

public static class Orchestration
{
    [Function(nameof(RequestChat))]
    public static async Task RequestChat(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(Orchestration));
        var beginChatToHuman = context.GetInput<FunctionPayloads.BeginChatToHuman>()!;

        var signalrChatIdentifier = beginChatToHuman.SignalrChatIdentifier;

        var agents = ((string, string)[])
        [
            //("ORCHESTRATOR", nameof(OrchestratorEntityAgent)),
            ("WRITER", nameof(WriterEntityAgent)),
            ("EDITOR", nameof(EditorEntityAgent)),
            ("HUMAN", nameof(UserAgentEntity)),
            ("IMPROVER", nameof(ImproverAgentEntity)),
            ("DIFFER", nameof(DifferEntityAgent))
        ];

        var agentMap = agents.ToDictionary(
            x => x.Item1,
            x => new EntityInstanceId(x.Item2, signalrChatIdentifier));

        await InitialiseAllAgentsWithChatIdentifier(context, agentMap, signalrChatIdentifier);

        var response = new AgentConversationTypes.AgentResponse(
            "WRITER",
            "HUMAN",
            "Let's start with an idea for a story");

        do
        {
            response = await RunAskOfSingleAgent(context, response, agentMap["HUMAN"], agentMap);
        } while (response.Next != "END");

        logger.LogInformation("Chat ended. SignalR chat identifier: {signalrChatIdentifier}", signalrChatIdentifier);
    }

    private static async Task<AgentConversationTypes.AgentResponse> RunAskOfSingleAgent(
        TaskOrchestrationContext context,
        AgentConversationTypes.AgentResponse request,
        EntityInstanceId humanEntityNewId,
        Dictionary<string, EntityInstanceId> agents)
    {

        var response = default(AgentConversationTypes.AgentResponse);
        var nextAgent = agents[request.Next];
        if (request.Next == "HUMAN")
        {
            //special agent where we go and wait for a response
            var random = context.NewGuid().ToString();
            var eventName = $"WaitForUserInput-{random}";
            response = await SignalHumanForResponse(context, request, humanEntityNewId, eventName);
        }
        else
        {
            response = await context.Entities.CallEntityAsync<AgentConversationTypes.AgentResponse>(
                nextAgent,
                nameof(LlmAgentEntity.GetResponse),
                (AgentConversationTypes.AgentResponse[]) [request]);
        }

        foreach (var entity in agents.Values)
        {
            await context.Entities.CallEntityAsync(
                entity,
                nameof(LlmAgentEntity.AgentHasSpoken),
                response);
        }

        return response;
    }

    private static async Task<AgentConversationTypes.AgentResponse> SignalHumanForResponse(
        TaskOrchestrationContext context,
        AgentConversationTypes.AgentResponse question,
        EntityInstanceId humanEntityNewId,
        string eventName)
    {
        await context.Entities.CallEntityAsync(
            humanEntityNewId,
            nameof(UserAgentEntity.AskQuestion),
            new AgentConversationTypes.AgentQuestionToHuman(
                eventName,
                question));

        var userResponse = await context.WaitForExternalEvent<FunctionPayloads.HumanResponseToAgentQuestion>(eventName);

        return await context.Entities.CallEntityAsync<AgentConversationTypes.AgentResponse>(
            humanEntityNewId,
            nameof(UserAgentEntity.RecordResponse),
            userResponse);
    }

    private static async Task InitialiseAllAgentsWithChatIdentifier(TaskOrchestrationContext context,
        Dictionary<string, EntityInstanceId> agents,
        string signalrChatIdentifier)
    {
        foreach (var kvp in agents)
        {
            await context.Entities.CallEntityAsync(
                kvp.Value,
                nameof(LlmAgentEntity.Init),
                new AgentState
                {
                    SignalrChatIdentifier = signalrChatIdentifier,
                    AgentName = kvp.Key
                });
        }
    }
}
