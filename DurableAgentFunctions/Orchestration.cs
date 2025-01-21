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
        var beginChatToHuman = context.GetInput<BeginChatToHuman>()!;

        var signalrChatIdentifier = beginChatToHuman.SignalrChatIdentifier;

        var orchestratorEntityId = new EntityInstanceId(nameof(ChatHistoryEntity), $"orchestratorhistory-{signalrChatIdentifier}");
        var writerEntityId = new EntityInstanceId(nameof(ChatHistoryEntity), $"writerhistory-{signalrChatIdentifier}");
        var editorEntityId = new EntityInstanceId(nameof(ChatHistoryEntity), $"editorhistory-{signalrChatIdentifier}");
        var userHistoryEntityId = new EntityInstanceId(nameof(ChatHistoryEntity), $"userhistory-{signalrChatIdentifier}");

        await context.Entities.CallEntityAsync(orchestratorEntityId, nameof(ChatHistoryEntity.Empty));
        await context.Entities.CallEntityAsync(writerEntityId, nameof(ChatHistoryEntity.Empty));

        var agents = new Dictionary<string, Type>()
        {
            ["FACILITATOR"] = typeof(OrchestratorAgent),
            ["HUMAN"] = typeof(UserAgent),
            ["WRITER"] = typeof(WriterAgent),
            ["EDITOR"] = typeof(EditorAgent),
        };
        
        var histories = new Dictionary<Type, EntityInstanceId>()
        {
            [typeof(OrchestratorAgent)] = orchestratorEntityId,
            [typeof(UserAgent)] = userHistoryEntityId,
            [typeof(WriterAgent)] = writerEntityId,
            [typeof(EditorAgent)] = editorEntityId,
        };
        
        var dispatchers = new Dictionary<Type, Func<TaskOrchestrationContext, Type, string, EntityInstanceId, AgentResponse?, Task<AgentResponse>>>()
        {
            [typeof(OrchestratorAgent)] = LlmAgent.HandleTurn,
            [typeof(UserAgent)] = UserAgent.HandleTurn,
            [typeof(WriterAgent)] = LlmAgent.HandleTurn,
            [typeof(EditorAgent)] = LlmAgent.HandleTurn,
        };
        
        var response = default(AgentResponse);
        do
        {
            var next = response == null ? "FACILITATOR" : response.Next;
            var agentType = agents[next];

            response = await dispatchers[agentType](
                context, 
                agentType, 
                signalrChatIdentifier,
                histories[agentType],
                response);
            
        } while (response.Next != "END");
        
        logger.LogInformation("Chat ended. SignalR chat identifier: {signalrChatIdentifier}", signalrChatIdentifier);
    }
}