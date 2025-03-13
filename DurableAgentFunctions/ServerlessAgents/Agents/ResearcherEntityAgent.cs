using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.Functions.Worker;

namespace DurableAgentFunctions.ServerlessAgents.Agents;

public class ResearcherEntityAgent: AgentEntity
{
    public ResearcherEntityAgent(HubConnection hubConnection) : base(hubConnection)
    {
    }
    

    [Function(nameof(ResearcherEntityAgent))]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<ResearcherEntityAgent>();
    }

    protected override Task<AgentConversationTypes.AgentResponse[]> GetResponseInternal(AgentConversationTypes.AgentResponse newMessageToAgent)
    {
        return Task.FromResult(new[]
        {
            new AgentConversationTypes.AgentResponse(
                "RESEARCHER",
                "WRITER",
                "It finished 2-0 to Liverpool. Goals were scored by Mo Salah and Dominik Szoboszlai. Liverpool are now 11 points clear at the top of the premier league. City are in freefall. they are now 4th. It's been a shocking season for them. Liverpool's manager is now Slot.")
        });
    }
}