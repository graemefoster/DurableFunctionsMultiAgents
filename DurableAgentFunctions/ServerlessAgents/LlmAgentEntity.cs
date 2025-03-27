using DurableAgentFunctions.ServerlessAgents.Planners;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents;

public abstract class LlmAgentEntity : AgentEntity
{
    private readonly IChatClient _chatClient;
    private readonly HubConnection _hubHubConnection;

    public LlmAgentEntity(IChatClient chatClient, HubConnection hubHubConnection) : base(hubHubConnection)
    {
        _chatClient = chatClient;
        _hubHubConnection = hubHubConnection;
    }

    protected abstract string SystemPrompt { get; }

    public HubConnection HubConnection => _hubHubConnection;

    protected override async Task<AgentConversationTypes.AgentResponse[]> GetResponseInternal(
        AgentConversationTypes.AgentResponse newMessageToAgent)
    {
        State.ChatHistory = State.ChatHistory.Concat([newMessageToAgent]).ToArray();
        
        IPlanner planner = State.PlannerType == "ORCHESTRATOR"
            ? new OrchestratorPlanner(State.AgentsICanTalkTo)
            : new NetworkPlanner(State.AgentsICanTalkTo);

        var agentChatHistory = BuildChatHistory(State.ChatHistory).ToArray();
        
        var messages = new[]
            {
                new ChatMessage(
                    ChatRole.System,
                    SystemPrompt +
                    Environment.NewLine + Environment.NewLine +
                    planner.GenerateRules(State.AgentName))
            }
            .Concat(agentChatHistory)
            .ToArray();

        var allResponses = new List<AgentConversationTypes.AgentResponse>();
        var response = await _chatClient.CompleteAsync(
            messages.ToList(),
            new ChatOptions()
            {
                Tools = GetCustomTools(allResponses)
                    .Concat(planner.GetCustomTools(State, allResponses)).ToArray(),
                ToolMode = ChatToolMode.RequireAny
            });

        await BroadcastPrompt(messages, allResponses);

        foreach (var agentRequest in allResponses.Where(x => x.Type == "MESSAGE"))
        {
            if (ShouldRetainInHistory(agentRequest))
            {
                State.ChatHistory = State.ChatHistory.Concat([agentRequest]).ToArray();
            }
        }

        return allResponses.ToArray();
    }

    protected virtual IEnumerable<AITool> GetCustomTools(IList<AgentConversationTypes.AgentResponse> responses)
    {
        return [];
    }

    protected virtual bool ShouldRetainInHistory(AgentConversationTypes.AgentResponse agentRequest) =>
        agentRequest.Type != "STORY";


    private async Task BroadcastPrompt(ChatMessage[] messages,
        IEnumerable<AgentConversationTypes.AgentResponse> responses)
    {
        await _hubHubConnection.InvokeAsync("BroadcastPrompt",
            State.SignalrChatIdentifier,
            State.AgentName,
            messages
                .Select(x => $"{x.Role.Value}: {x.Text}")
                .Union(responses.Select(response =>
                    $"LLM RESPONSE -> {response.Type} - {response.Next}: {response.Message}"))
                .ToArray());
    }

    protected virtual IEnumerable<ChatMessage> BuildChatHistory(
        IEnumerable<AgentConversationTypes.AgentResponse> history)
    {
        //Only improve on the current story. Don't bother with anything else
        if (!string.IsNullOrEmpty(State.CurrentStory))
        {
            yield return new ChatMessage(ChatRole.Assistant, "Here is the OLD story:");
            yield return new ChatMessage(ChatRole.Assistant, base.State.CurrentStory);
        }
        else
        {
            yield return new ChatMessage(ChatRole.Assistant, "There is NO current story");
        }

        foreach (var message in history)
        {
            yield return new ChatMessage(
                message.From.Equals("HUMAN", StringComparison.InvariantCultureIgnoreCase)
                    ? ChatRole.User
                    : ChatRole.Assistant,
                $"From:{message.From} to {message.Next} - {message.Message}");
        }
    }

}