using Microsoft.Extensions.AI;

namespace DurableAgentFunctions;

public record BeginChatToHuman(string SignalrChatIdentifier);
public record AgentUpdateToHuman(string SignalrChatIdentifier, string Result);

public record RequestToAgent(AgentResponse[] ChatHistory, string AgentType);

public record PostProcessAgentResponse(string SignalrChatIdentifier, AgentResponse[] ChatHistory, AgentResponse Response, string AgentType);
public record AgentQuestionToHuman(string SignalrChatIdentifier, string EventName, string From, string Question);
public record HumanResponseToAgentQuestion(string InstanceId, string EventName, string Response);

public record AgentSettings(string SignalrUrl, string AoaiUrl, string AoaiKey, string AoaiDeploymentName);
