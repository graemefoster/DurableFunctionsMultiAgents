using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DurableAgentFunctions.ServerlessAgents;

public abstract class LlmAgent
{
    protected abstract string SystemPrompt { get; } 
    private readonly IChatClient _chatClient;

    protected LlmAgent(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    protected virtual IEnumerable<ChatMessage> BuildChatHistory(IEnumerable<AgentResponse> history) => 
        history.Select(x => new ChatMessage(x.From.Equals("HUMAN", StringComparison.InvariantCultureIgnoreCase) ? ChatRole.User : ChatRole.Assistant, x.Message));
    
    private async Task<AgentResponse> GetResponse(IEnumerable<AgentResponse> history)
    {
        var messages = new[]
            {
                new ChatMessage(ChatRole.System, SystemPrompt)
            }
            .Concat(BuildChatHistory(history))
            .ToArray();

        var response = await _chatClient.CompleteAsync(
            messages.ToList(),
            new ChatOptions()
            {
                ResponseFormat = ChatResponseFormat.Json
            });
        
        return JsonConvert.DeserializeObject<AgentResponse>(response.Message.Text!)!;
    }

    protected virtual Task ApplyAgentCustomLogic(
        FunctionContext context, 
        PostProcessAgentResponse response) =>
        Task.CompletedTask;

    [Function(nameof(AgentCustomLogicActivityFunction))]
    public static Task AgentCustomLogicActivityFunction(
        [ActivityTrigger] PostProcessAgentResponse response,
        FunctionContext executionContext)
    {
        var chatClient = (LlmAgent)executionContext.InstanceServices.GetRequiredService(Type.GetType(response.AgentType)!);
        return chatClient.ApplyAgentCustomLogic(executionContext, response);
    }

    public static async Task<AgentResponse> HandleTurn(
        TaskOrchestrationContext context, 
        Type agentType,
        string signalrChatIdentifier,
        EntityInstanceId agentsHistoryEntityId, 
        AgentResponse? responseFromLastAgent)
    {
        if (responseFromLastAgent != null)
        {
            context.CreateReplaySafeLogger<WriterAgent>()
                .LogInformation("Adding to orchestrator history. From:{from}. Message:{message}",
                    responseFromLastAgent.From, responseFromLastAgent.Message);

            await context.Entities.CallEntityAsync(
                agentsHistoryEntityId,
                nameof(ChatHistoryEntity.Add),
                responseFromLastAgent);
        }

        var agentHistory = await context.Entities.CallEntityAsync<AgentResponse[]>(
            agentsHistoryEntityId,
            nameof(ChatHistoryEntity.GetHistory));

        var replaySafeLogger = context.CreateReplaySafeLogger<LlmAgent>();
        
        replaySafeLogger.LogInformation(string.Join('\n', agentHistory.Select(x => $"{x.From}, {x.Next}, {x.Message}")));

        var response = await context.CallActivityAsync<AgentResponse>(
            nameof(ChatToAgentActivityFunction),
            new RequestToAgent(agentHistory, agentType.FullName!));

        replaySafeLogger
            .LogInformation("Orchestrator response: {to}:{message}", response.Next, response.Message);

        await context.Entities.CallEntityAsync(
            agentsHistoryEntityId,
            nameof(ChatHistoryEntity.Add),
            response);

        //Give an agent chance to do some custom logic
        await context.CallActivityAsync(
            nameof(AgentCustomLogicActivityFunction),
            new PostProcessAgentResponse(signalrChatIdentifier, agentHistory, response, agentType.FullName!));

        return response;
    }
    
    [Function(nameof(ChatToAgentActivityFunction))]
    public static Task<AgentResponse> ChatToAgentActivityFunction(
        [ActivityTrigger] RequestToAgent request,
        FunctionContext executionContext)
    {
        var chatClient = (LlmAgent)executionContext.InstanceServices.GetRequiredService(Type.GetType(request.AgentType)!);
        return chatClient.GetResponse(request.ChatHistory);
    }

}