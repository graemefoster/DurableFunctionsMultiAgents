using DurableAgentFunctions.ServerlessAgents;
using DurableAgentFunctions.ServerlessAgents.Agents;
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

        int turnsSoFar = 0;

        var agents =
            new Dictionary<string, (string, string[], string)>()
            {
                ["END"] = (nameof(EndAgentEntity), [],
                    "I will end the project. Only call me when the HUMAN has indicated that NO MORE changes are needed to the story"),
                ["WRITER"] = (nameof(WriterEntityAgent), ["IMPROVER", "RESEARCHER", "HUMAN", "END"],
                    "I can write stories based on the provided information."),
                ["RESEARCHER"] = (nameof(ResearcherEntityAgent), [],
                    "I will search the Internet to find relevant information to help write the story - if it refers to recent real events."),
                ["HUMAN"] = (nameof(UserAgentEntity), [], "I can provide answers to questions from the agents to improve the story, or provide more background to it."),
                ["IMPROVER"] = (nameof(ImproverAgentEntity), ["HUMAN", "WRITER"],
                    "I look at stories published by the WRITER, and ask thought provoking questions to the HUMAN to improve the story."),
                ["DIFFER"] = (nameof(DifferEntityAgent), ["WRITER"],
                    "I can analyse text-diffs that are edits made to a story by the HUMAN, and will tell the WRITER what needs to be changed."),
                ["ORCHESTRATOR"] = (nameof(OrchestratorEntityAgent), ["IMPROVER", "WRITER", "RESEARCHER", "END", "HUMAN"],
                    "I orchestrate the conversation and decide who gets involved next."),
            };

        var useOrchestrator = Environment.GetEnvironmentVariable("PLANNER_TYPE") == "ORCHESTRATOR";

        var agentMap = agents.ToDictionary(
            x => x.Key,
            x => (
                new AgentState()
                {
                    AgentName = x.Key,
                    AgentsICanTalkTo = x.Value.Item2.Select(x => new FriendAgent() { Name = x, Capability = agents[x].Item3 }).ToArray(),
                    AgentSummary = x.Value.Item3,
                    SignalrChatIdentifier = signalrChatIdentifier,
                    PlannerType = useOrchestrator ? "ORCHESTRATOR" : "NETWORK"
                }, 
                new EntityInstanceId(x.Value.Item1, signalrChatIdentifier)));

        var agentEntityIds = agentMap.ToDictionary(x => x.Key, x => x.Value.Item2);

        await InitialiseAllAgentsWithChatIdentifier(context, agentMap);

        var responses = new[]
        {
            new AgentConversationTypes.AgentResponse(
                "MESSAGE",
                DateTimeOffset.Now, 
                useOrchestrator ? "ORCHESTRATOR": "WRITER",
                "HUMAN",
                "Let's start with an idea for a story")
        };

        do
        {
            
            responses = await RunAskOfAgents(context, responses, agentMap["HUMAN"].Item2, agentEntityIds);
            turnsSoFar += responses.Length; 

        } while (turnsSoFar < 20 && responses.All(x => x.Next != "END"));

        logger.LogInformation("Chat ended. SignalR chat identifier: {signalrChatIdentifier}", signalrChatIdentifier);
    }

    private static async Task<AgentConversationTypes.AgentResponse[]> RunAskOfAgents(
        TaskOrchestrationContext context,
        AgentConversationTypes.AgentResponse[] requests,
        EntityInstanceId humanEntityNewId,
        Dictionary<string, EntityInstanceId> agents)
    {
        
        var allResponses = await Task.WhenAll(requests.Select(x => RunAskOfSingleAgent(context, x, humanEntityNewId, agents)));
        return allResponses.SelectMany(x => x).ToArray();
    }

    private static async Task<AgentConversationTypes.AgentResponse[]> RunAskOfSingleAgent(
        TaskOrchestrationContext context,
        AgentConversationTypes.AgentResponse request,
        EntityInstanceId humanEntityNewId,
        Dictionary<string, EntityInstanceId> agents)
    {
        AgentConversationTypes.AgentResponse[] responses;

        var nextAgent = agents[request.Next];
        if (request.Next == "HUMAN")
        {
            //special agent where we go and wait for a response
            var random = context.NewGuid().ToString();
            var eventName = $"WaitForUserInput-{random}";
            responses = [await SignalHumanForResponse(context, request, humanEntityNewId, eventName)];
        }
        else
        {
            responses = await context.Entities.CallEntityAsync<AgentConversationTypes.AgentResponse[]>(
                nextAgent,
                nameof(AgentEntity.GetResponse),
                request);
        }

        foreach (var entity in agents.Values)
        {
            foreach (var response in responses)
            {
                await context.Entities.CallEntityAsync(
                    entity,
                    nameof(AgentEntity.AgentHasSpoken),
                    response);
            }
        }

        return responses.Where(x => x.Type == "MESSAGE").ToArray();
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

    private static async Task InitialiseAllAgentsWithChatIdentifier(
        TaskOrchestrationContext context,
        Dictionary<string, (AgentState, EntityInstanceId)> agents)
    {
        foreach (var kvp in agents)
        {
            await context.Entities.CallEntityAsync(
                kvp.Value.Item2,
                nameof(AgentEntity.Init),
                kvp.Value.Item1
                );
        }
    }
}