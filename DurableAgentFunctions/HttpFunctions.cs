using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DurableTask.Client;

namespace DurableAgentFunctions;

public class HttpFunctions
{
    private readonly ILogger<HttpFunctions> _logger;
    private readonly DurableTaskClient _durableTaskClient;

    public HttpFunctions(ILogger<HttpFunctions> logger, DurableTaskClient durableTaskClient)
    {
        _logger = logger;
        _durableTaskClient = durableTaskClient;
    }

    [Function("StartNewChat")]
    public async Task<IActionResult> StartNewChat(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        [Microsoft.Azure.Functions.Worker.Http.FromBody]FunctionPayloads.BeginChatToHuman startContext)
    {
        _logger.LogInformation("Starting durable function. SignalR ConnectionId: {SignalrConnectionId}", startContext.SignalrChatIdentifier);
        
        //start a durable function that will end by notifying the SignalR hub
        string instanceId = await _durableTaskClient.ScheduleNewOrchestrationInstanceAsync(
            nameof(Orchestration.RequestChat),
            startContext);
        
        return new OkObjectResult(instanceId);
        
    }

    [Function("CancelChat")]
    [Route("CancelChat/{instanceId}")]
    public async Task<IActionResult> CancelChat(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
        string instanceId)
    {
        await _durableTaskClient.TerminateInstanceAsync(instanceId);
        
        return new OkObjectResult(instanceId);
        
    }
    
    [Function("ResponseToQuestion")]
    public async Task<IActionResult> ResponseToQuestion(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        [Microsoft.Azure.Functions.Worker.Http.FromBody] FunctionPayloads.HumanResponseToAgentQuestion question)
    {
        _logger.LogInformation("Received response to question. InstanceId: {InstanceId}, EventName: {EventName}, Response: {Response}", question.InstanceId, question.EventName, question.Response);
        await _durableTaskClient.RaiseEventAsync(question.InstanceId, question.EventName, question);
        return new OkResult();
    }

}