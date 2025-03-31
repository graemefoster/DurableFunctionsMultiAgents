using Microsoft.SemanticKernel;

namespace SemanticKernelWriterMultiAgent;

public class GroupChatCache
{
#pragma warning disable SKEXP0110
    public readonly IDictionary<string, IList<ChatMessageContent>> CachedChats = new Dictionary<string, IList<ChatMessageContent>>();
#pragma warning restore SKEXP0110
}