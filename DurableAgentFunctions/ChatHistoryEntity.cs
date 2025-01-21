using Microsoft.Azure.Functions.Worker;

namespace DurableAgentFunctions;

public class ChatHistoryEntity
{
    public AgentResponse[] History { get; set; } = [];
    
    public void Empty()
    {
        History = [];
    }    
    
    public void Add(AgentResponse message)
    {
        History = History.Append(message).ToArray();
    }    
    
    public AgentResponse[] GetHistory() =>  History;
    
    [Function(nameof(ChatHistoryEntity))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<ChatHistoryEntity>();
    }
}