using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents;

public abstract class LlmAgentEntity : AgentEntity
{
    private readonly IChatClient _chatClient;
    private readonly HubConnection _hubHubConnection;

    public LlmAgentEntity(IChatClient chatClient, HubConnection hubHubConnection): base(hubHubConnection)
    {
        _chatClient = chatClient;
        _hubHubConnection = hubHubConnection;
    }
    
    protected abstract string SystemPrompt { get; }

    public HubConnection HubConnection => _hubHubConnection;

    protected override async Task<AgentConversationTypes.AgentResponse[]> GetResponseInternal(AgentConversationTypes.AgentResponse newMessageToAgent)
    {
        State.ChatHistory = State.ChatHistory.Concat([newMessageToAgent]).ToArray();

        var messages = new[]
            {
                new ChatMessage(ChatRole.System, SystemPrompt + 
                    $$""""

**RULES**
---------
You can only send a message to ONE agent and they MUST be listed below. IF YOU TRY TO TALK TO MORE, WE WILL ONLY USE THE FIRST ONE.

The available agents are:
{{string.Join($"{Environment.NewLine}", State.AgentsICanTalkTo.Select(x => $"{x.Name} - {x.Capability}"))}}.

Remember you cannot talk DIRECTLY to any other agent than the listed ones.

"""")
            }
            .Concat(BuildChatHistory(State.ChatHistory))
            .ToArray();

        var allResponses = new List<AgentConversationTypes.AgentResponse>();
        var response = await _chatClient.CompleteAsync(
            messages.ToList(),
            new ChatOptions()
            {
                Tools = GetCustomTools(allResponses).Concat([ 
                    AIFunctionFactory.Create(
                        (string nextAgent, string message) => SendMessageToAgent(allResponses, nextAgent, message),
                        new AIFunctionFactoryCreateOptions()
                        {
                            Name = nameof(SendMessageToAgent),
                            Description = "Send a message to another agent"
                        })]).ToArray(),
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

    protected virtual bool ShouldRetainInHistory(AgentConversationTypes.AgentResponse agentRequest) => agentRequest.Type != "STORY";


    private async Task BroadcastPrompt(ChatMessage[] messages, IEnumerable<AgentConversationTypes.AgentResponse> responses)
    {
        foreach (var response in responses)
        {
            await _hubHubConnection.InvokeAsync("BroadcastPrompt",
                State.SignalrChatIdentifier,
                State.AgentName,
                messages
                    .Select(x => $"{x.Role.Value}: {x.Text}")
                    .Union([$"LLM RESPONSE -> {response.Next}: {response.Message}"])
                    .ToArray());
        }
    }

    // protected virtual Task<AgentConversationTypes.AgentResponse> ApplyAgentCustomLogic(
    //     AgentConversationTypes.AgentResponse agentResponse) => Task.FromResult(agentResponse);

    protected virtual IEnumerable<ChatMessage> BuildChatHistory(
        IEnumerable<AgentConversationTypes.AgentResponse> history)
    {
        foreach (var message in history)
        {
            yield return new ChatMessage(
                message.From.Equals("HUMAN", StringComparison.InvariantCultureIgnoreCase)
                    ? ChatRole.User
                    : ChatRole.Assistant,
                $"From:{message.From} to {message.Next} - {message.Message}");
        }
        
        //Only improve on the current story. Don't bother with anything else
        if (State.CurrentStory != null)
        {
            yield return  new ChatMessage(ChatRole.Assistant, "Latest Story Follows:");
            yield return new ChatMessage(ChatRole.Assistant, base.State.CurrentStory);
        }
        else
        {
            yield return new ChatMessage(ChatRole.Assistant, "There is NO current story");
        }
    }

    private void SendMessageToAgent(IList<AgentConversationTypes.AgentResponse> responses, string nextAgent, string message)
    {
        responses.Add(new AgentConversationTypes.AgentResponse("MESSAGE", State.AgentName, nextAgent.ToUpperInvariant(), message));
    }
}