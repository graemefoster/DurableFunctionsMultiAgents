using DurableAgentFunctions.ServerlessAgents;
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

        // var orchestratorEntityNewId = new EntityInstanceId(nameof(OrchestratorEntityAgent), signalrChatIdentifier);
        var writerEntityNewId = new EntityInstanceId(nameof(WriterEntityAgent), signalrChatIdentifier);
        var editorEntityNewId = new EntityInstanceId(nameof(EditorEntityAgent), signalrChatIdentifier);
        var humanEntityNewId = new EntityInstanceId(nameof(UserAgentEntity), signalrChatIdentifier);
        var improverEntityNewId = new EntityInstanceId(nameof(ImproverAgentEntity), signalrChatIdentifier);
        var differEntityNewId = new EntityInstanceId(nameof(DifferEntityAgent), signalrChatIdentifier);

        var agents = new Dictionary<string, EntityInstanceId>()
        {
            // ["FACILITATOR"] = orchestratorEntityNewId,
            ["WRITER"] = writerEntityNewId,
            ["EDITOR"] = editorEntityNewId,
            ["HUMAN"] = humanEntityNewId,
            ["IMPROVER"] = improverEntityNewId,
            ["DIFFER"] = differEntityNewId
        };

        await InitialiseAllAgentsWithChatIdentifier(context, agents, signalrChatIdentifier);

        var response = new AgentConversationTypes.AgentResponse(
            "WRITER",
            "HUMAN",
            "Let's start with an idea for a story");

        do
        {
            var nextAgentTasks = new List<Task<AgentConversationTypes.AgentResponse>>();
            if (response.Next.Length == 1)
            {
                await RunAskOfSingleAgent(context, response, nextAgentTasks, humanEntityNewId, agents, signalrChatIdentifier);
            }
        } while (response.Next != "END");

        logger.LogInformation("Chat ended. SignalR chat identifier: {signalrChatIdentifier}", signalrChatIdentifier);
    }

    private static async Task<AgentConversationTypes.AgentResponse> RunAskOfSingleAgent(
        TaskOrchestrationContext context, 
        AgentConversationTypes.AgentResponse response,
        List<Task<AgentConversationTypes.AgentResponse>> nextAgentTasks, 
        EntityInstanceId humanEntityNewId, 
        Dictionary<string, EntityInstanceId> agents, 
        string signalrChatIdentifier)
    {
        
        if (response.Next == "HUMAN")
        {
            //special agent where we go and wait for a response
            var random = context.NewGuid().ToString();
            var eventName = $"WaitForUserInput-{random}";
            nextAgentTasks.Add(SignalHumanForResponse(context, response, humanEntityNewId, eventName));
        }
        else
        {
            var agentEntityId = agents[response.Next];
            nextAgentTasks.Add(context.Entities.CallEntityAsync<AgentConversationTypes.AgentResponse>(
                agentEntityId,
                nameof(LlmAgentEntity.GetResponse),
                (AgentConversationTypes.AgentResponse[]) [response]));
        }


        foreach (var entity in agents.Values)
        {
            await context.Entities.CallEntityAsync(
                entity,
                nameof(LlmAgentEntity.AgentHasSpoken),
                response);
        }

        if (response.Next != "HUMAN")
        {
            await context.CallActivityAsync(
                nameof(BroadcastInternalConversationPiece),
                new FunctionPayloads.AgentChitChat(signalrChatIdentifier, response));
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
            await context.Entities.CallEntityAsync(kvp.Value, nameof(LlmAgentEntity.Init), new AgentState
            {
                SignalrChatIdentifier = signalrChatIdentifier,
                AgentName = kvp.Key
            });
        }
    }

    [Function(nameof(BroadcastInternalConversationPiece))]
    public static async Task BroadcastInternalConversationPiece(
        [ActivityTrigger] FunctionPayloads.AgentChitChat request,
        FunctionContext context)
    {
        var hubConnection = context.InstanceServices.GetRequiredService<HubConnection>();
        await hubConnection.InvokeAsync(
            "AgentChitChat",
            request.SignalrChatIdentifier,
            request.Response.From,
            request.Response.Next,
            request.Response.Message);
    }
}