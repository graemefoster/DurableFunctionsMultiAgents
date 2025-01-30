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

        var orchestratorEntityNewId = new EntityInstanceId(nameof(OrchestratorEntityAgent), signalrChatIdentifier);
        var writerEntityNewId = new EntityInstanceId(nameof(WriterEntityAgent), signalrChatIdentifier);
        var editorEntityNewId = new EntityInstanceId(nameof(EditorEntityAgent), signalrChatIdentifier);
        var humanEntityNewId = new EntityInstanceId(nameof(UserAgentEntity), signalrChatIdentifier);
        var improverEntityNewId = new EntityInstanceId(nameof(ImproverAgentEntity), signalrChatIdentifier);

        var agents = new Dictionary<string, EntityInstanceId>()
        {
            ["FACILITATOR"] = orchestratorEntityNewId,
            ["WRITER"] = writerEntityNewId,
            ["EDITOR"] = editorEntityNewId,
            ["HUMAN"] = humanEntityNewId,
            ["IMPROVER"] = improverEntityNewId
        };

        foreach (var kvp in agents)
        {
            await context.Entities.CallEntityAsync(kvp.Value, nameof(LlmAgentEntity.Init), new AgentState
            {
                SignalrChatIdentifier = signalrChatIdentifier,
                AgentName = kvp.Key
            });
        }

        var response = default(AgentConversationTypes.AgentResponse);
        do
        {
            var nextAgent = response == null ? "FACILITATOR" : response.Next;
            if (nextAgent == "HUMAN")
            {
                var random = context.NewGuid().ToString();
                var eventName = $"WaitForUserInput-{random}";

                await context.Entities.CallEntityAsync(
                    humanEntityNewId,
                    nameof(UserAgentEntity.AskQuestion),
                    new AgentConversationTypes.AgentQuestionToHuman(
                        eventName,
                        response!));

                var userResponse = await context.WaitForExternalEvent<string>(eventName);

                response = await context.Entities.CallEntityAsync<AgentConversationTypes.AgentResponse>(
                    humanEntityNewId,
                    nameof(UserAgentEntity.RecordResponse),
                    userResponse);
            }
            else
            {
                var agentEntityId = agents[nextAgent];
                var inputMessages = response != null ? (AgentConversationTypes.AgentResponse[]) [response] : [];

                response = await context.Entities.CallEntityAsync<AgentConversationTypes.AgentResponse>(
                    agentEntityId,
                    nameof(LlmAgentEntity.GetResponse),
                    inputMessages);

                foreach (var entity in agents.Values.Except([agents[nextAgent]]))
                {
                    await context.Entities.CallEntityAsync(
                        entity,
                        nameof(LlmAgentEntity.AgentHasSpoken),
                        response);
                }
                {
                    response = await context.Entities.CallEntityAsync<AgentConversationTypes.AgentResponse>(
                        agentEntityId,
                        nameof(LlmAgentEntity.GetResponse),
                        inputMessages);
                }
                
                if (response.Next != "HUMAN")
                {
                    await context.CallActivityAsync(
                        nameof(BroadcastInternalConversationPiece),
                        new FunctionPayloads.AgentChitChat(signalrChatIdentifier, response));
                }
            }
        } while (response.Next != "END");

        logger.LogInformation("Chat ended. SignalR chat identifier: {signalrChatIdentifier}", signalrChatIdentifier);
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