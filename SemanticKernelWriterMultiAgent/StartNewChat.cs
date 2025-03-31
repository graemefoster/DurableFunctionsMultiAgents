using DurableAgentFunctions;
using DurableAgentFunctions.ServerlessAgents;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticKernelWriterMultiAgent.Agents;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace SemanticKernelWriterMultiAgent;
#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001

public class StartNewChat
{
    private readonly ILogger<StartNewChat> _logger;
    private readonly Kernel _kernel;
    private readonly GroupChatCache _groupChatCache;

    public StartNewChat(ILogger<StartNewChat> logger, Kernel kernel, GroupChatCache groupChatCache)
    {
        _logger = logger;
        _kernel = kernel;
        _groupChatCache = groupChatCache;
    }

    [Function("StartNewChat")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req,
        [Microsoft.Azure.Functions.Worker.Http.FromBody] FunctionPayloads.BeginChatToHuman startContext)
    {
        _logger.LogInformation("Starting durable function. SignalR ConnectionId: {SignalrConnectionId}",
            startContext.SignalrChatIdentifier);

        var sessionId = Guid.NewGuid().ToString();
        var chat = CreateAgentChat(_kernel);
        var chatMessageContents = new List<ChatMessageContent>();
        _groupChatCache.CachedChats[sessionId] = chatMessageContents;

        var responses = await RunTheAgents(chat, chatMessageContents);

        //we are running chats in the background. Let's use a queue to schedule the chat to run.
        return new OkObjectResult(new
        {
            sessionId = sessionId,
            responses = responses
        });
    }

    private static async Task<List<AgentConversationTypes.AgentResponse>> RunTheAgents(AgentChat chat, IList<ChatMessageContent> theChat)
    {
        var responses = new List<AgentConversationTypes.AgentResponse>();
        AgentConversationTypes.AgentResponse lastResponse = default;
        await foreach (var chatResponse in chat.InvokeAsync())
        {
            theChat.Add(chatResponse);

            lastResponse = new AgentConversationTypes.AgentResponse(
                "MESSAGE", 
                DateTimeOffset.Now, 
                chatResponse.AuthorName,
                "HUMAN",
                chatResponse.Content);
            
            responses.Add(lastResponse);
        }

        return responses;
    }


    [Function("ResponseToQuestion")]
    public async Task<IActionResult> ResponseToQuestion([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req,
        [Microsoft.Azure.Functions.Worker.Http.FromBody]
        FunctionPayloads.HumanResponseToAgentQuestion response)
    {
        _logger.LogInformation("Starting durable function. SignalR ConnectionId: {SignalrConnectionId}",
            response.InstanceId);

        var chat = _groupChatCache.CachedChats[response.InstanceId];
        var agentChat = CreateAgentChat(_kernel);
        foreach(var message in chat)
        {
            agentChat.AddChatMessage(message);
        }
        agentChat.AddChatMessage(new ChatMessageContent(AuthorRole.User, response.Response));

        var responses = await RunTheAgents(agentChat, chat);

        //we are running chats in the background. Let's use a queue to schedule the chat to run.
        return new OkObjectResult(new
        {
            sessionId = response.InstanceId,
            responses = responses
        });

    }

    private AgentChat CreateAgentChat(Kernel kernel)
    {
        var agents =
            new Dictionary<string, string>()
            {
                ["WRITER"] = "I can write stories based on the provided information.",
                // ["RESEARCHER"] = "I will search the Internet to find relevant information to help write the story - if it refers to recent real events.",
                // ["HUMAN"] = "I can provide answers to questions from the agents to improve the story, or provide more background to it.",
                ["IMPROVER"] =
                    "I look at stories published by the WRITER, and ask thought provoking questions to the HUMAN to improve the story.",
                ["DIFFER"] =
                    "I can analyse text-diffs that are edits made to a story by the HUMAN, and will tell the WRITER what needs to be changed.",
            };

        var writer = WriterEntityAgent.Build(kernel);
        var differ = WriterEntityAgent.Build(kernel);
        var improver = ImproverEntityAgent.Build(kernel);

        var historyReducer = new ChatHistoryTruncationReducer(1);

        const string TerminationToken = "yes";
        var orchestrator = OrchestrationControl.BuildNextAgentFunction(agents);
        var terminator = OrchestrationControl.BuildTerminationCheckFunction(agents, TerminationToken);
        var chat =
            new AgentGroupChat(writer, differ, improver)
            {
                ExecutionSettings = new AgentGroupChatSettings
                {
                    SelectionStrategy =
                        new KernelFunctionSelectionStrategy(orchestrator, kernel)
                        {
                            // Always start with the writer agent.
                            InitialAgent = writer,
                            // Save tokens by only including the final response
                            HistoryReducer = historyReducer,
                            // The prompt variable name for the history argument.
                            HistoryVariableName = "lastmessage",
                            // Returns the entire result value as a string.
                            ResultParser = (result) => result.GetValue<string>() ?? writer.Name!
                        },
                    TerminationStrategy =
                        new KernelFunctionTerminationStrategy(terminator, kernel)
                        {
                            // Only evaluate for editor's response
                            Agents = [writer],
                            // Save tokens by only including the final response
                            HistoryReducer = historyReducer,
                            // The prompt variable name for the history argument.
                            HistoryVariableName = "lastmessage",
                            // Limit total number of turns
                            MaximumIterations = 12,
                            // Customer result parser to determine if the response is "yes"
                            ResultParser = result =>
                                result.GetValue<string>()
                                    ?.Contains(TerminationToken, StringComparison.OrdinalIgnoreCase) ?? false
                        }
                }
            };

        return chat;
    }
}

#pragma warning restore SKEXP0110
#pragma warning restore SKEXP0001

