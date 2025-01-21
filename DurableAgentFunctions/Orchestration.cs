using DurableAgentFunctions.ServerlessAgents;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Entities;
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

        var agents = new Dictionary<string, EntityInstanceId>()
        {
            ["FACILITATOR"] = orchestratorEntityNewId,
            ["WRITER"] = writerEntityNewId,
            ["EDITOR"] = editorEntityNewId,
            ["HUMAN"] = humanEntityNewId
        };

        foreach (var kvp in agents)
        {
            await context.Entities.CallEntityAsync(kvp.Value, nameof(LlmAgentEntity.Init), new AgentState
            {
                SignalrChatIdentifier = signalrChatIdentifier
            });
        }
        
        var response = default(AgentResponse);
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
                    new AgentQuestionToHuman(
                        eventName,
                        response!));

                var userResponse = await context.WaitForExternalEvent<string>(eventName);

                response = await context.Entities.CallEntityAsync<AgentResponse>(
                    humanEntityNewId,
                    nameof(UserAgentEntity.RecordResponse),
                    userResponse);
            }
            else
            {
                var agentEntityId = agents[nextAgent];
                var inputMessages = response != null ? (AgentResponse[])[response] : [];
                response = await context.Entities.CallEntityAsync<AgentResponse>(
                    agentEntityId,
                    nameof(LlmAgentEntity.GetResponse),
                    inputMessages);
            }
        } while (response.Next != "END");

        logger.LogInformation("Chat ended. SignalR chat identifier: {signalrChatIdentifier}", signalrChatIdentifier);
    }
}