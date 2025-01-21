using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace DurableAgentFunctions.ServerlessAgents;

public class UserAgent
{
    public static async Task<AgentResponse> HandleTurn(
        TaskOrchestrationContext context,
        Type agentType,
        string signalrChatIdentifier,
        EntityInstanceId agentsHistoryEntityId,
        AgentResponse? responseFromLastAgent)
    {
        var random = context.NewGuid().ToString();
        var eventName = $"WaitForUserInput-{random}";

        await context.CallActivityAsync(nameof(AskQuestion),
            new AgentQuestionToHuman(signalrChatIdentifier, eventName, responseFromLastAgent!.From, responseFromLastAgent!.Message));

        var userResponse = await context.WaitForExternalEvent<string>(eventName);

        return new AgentResponse("HUMAN", "FACILITATOR", userResponse);
    }   
    
    [Function("AskQuestion")]
    public static async Task AskQuestion(
        [ActivityTrigger] AgentQuestionToHuman questionToHuman,
        FunctionContext executionContext)
    {
        var signalrHub = executionContext.InstanceServices.GetRequiredService<HubConnection>();

        await signalrHub.InvokeAsync(
            "AskForUserInput",
            questionToHuman.SignalrChatIdentifier,
            questionToHuman.From,
            questionToHuman.Question,
            questionToHuman.EventName);
    }

}