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
                    "I will stop the conversation if everyone is happy!"),
                ["WRITER"] = (nameof(WriterEntityAgent), ["IMPROVER", "RESEARCHER", "END"],
                    "I can write stories based on the provided information."),
                ["RESEARCHER"] = (nameof(ResearcherEntityAgent), [],
                    "I will search the Internet to find relevant information to shape the story."),
                // ["EDITOR"] = (nameof(EditorEntityAgent), ["IMPROVER"],
                //     "I will check the grammar and punctuation of the writer's story for them."),
                ["HUMAN"] = (nameof(UserAgentEntity), [], ""),
                ["IMPROVER"] = (nameof(ImproverAgentEntity), ["HUMAN", "WRITER"],
                    "I can interact with a HUMAN to get more information to improve the story."),
                ["DIFFER"] = (nameof(DifferEntityAgent), ["WRITER"],
                    "I can look at edits made to a story by the HUMAN, and tell the WRITER what needs to be changed."),
            };

        var agentMap = agents.ToDictionary(
            x => x.Key,
            x => (
                new AgentState()
                {
                    AgentName = x.Key,
                    AgentsICanTalkTo = x.Value.Item2.Select(x => new FriendAgent() { Name = x, Capability = agents[x].Item3 }).ToArray(),
                    AgentSummary = x.Value.Item3,
                    SignalrChatIdentifier = signalrChatIdentifier
                }, 
                new EntityInstanceId(x.Value.Item1, signalrChatIdentifier)));

        var agentEntityIds = agentMap.ToDictionary(x => x.Key, x => x.Value.Item2);

        await InitialiseAllAgentsWithChatIdentifier(context, agentMap);

        var responses = new[]
        {
            new AgentConversationTypes.AgentResponse(
                "WRITER",
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

        return responses;
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