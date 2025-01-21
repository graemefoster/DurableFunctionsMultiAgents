using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.AI;
using Newtonsoft.Json;

namespace DurableAgentFunctions.ServerlessAgents;

public abstract class LlmAgentEntity : TaskEntity<AgentState>
{
    private readonly IChatClient _chatClient;

    public LlmAgentEntity(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }
    
    protected abstract string SystemPrompt { get; }

    public void Init(AgentState state)
    {
        State = state;
    }

    public async Task<AgentResponse> GetResponse(params AgentResponse[] newMessagesToAgent)
    {
        if (newMessagesToAgent.Any())
        {
            State.ChatHistory = State.ChatHistory.Concat(newMessagesToAgent).ToArray();
        }

        var messages = new[]
            {
                new ChatMessage(ChatRole.System, SystemPrompt)
            }
            .Concat(BuildChatHistory(State.ChatHistory))
            .ToArray();

        var response = await _chatClient.CompleteAsync(
            messages.ToList(),
            new ChatOptions()
            {
                ResponseFormat = ChatResponseFormat.Json
            });
        
        var agentResponse = JsonConvert.DeserializeObject<AgentResponse>(response.Message.Text!)!;
        await ApplyAgentCustomLogic(agentResponse);

        return agentResponse;
    }

    protected virtual Task ApplyAgentCustomLogic(
        AgentResponse agentResponse) =>
        Task.CompletedTask;

    protected virtual IEnumerable<ChatMessage> BuildChatHistory(IEnumerable<AgentResponse> history) => 
        history.Select(x => new ChatMessage(x.From.Equals("HUMAN", StringComparison.InvariantCultureIgnoreCase) ? ChatRole.User : ChatRole.Assistant, x.Message));

}