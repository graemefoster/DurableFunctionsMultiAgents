using Microsoft.Extensions.AI;

namespace DurableAgentFunctions.ServerlessAgents;

public class WriterAgent: LlmAgent
{
    public WriterAgent(IChatClient chatClient) : base(chatClient)
    {
    }

    protected override string SystemPrompt =>
        """
        You are a fabulous writer. 
        You will work with the human and an expert editor, to write a story, taking into account all editor and human Feedback.
        
        You must have enough information from the User before starting to write a story. Ask them for more information if needed.

        Respond with JSON in the following format: 
        {
            "from": "WRITER",
            "next": "EDITOR",
            "message": "...The story..."
        }
        
        "next" must be either EDITOR or HUMAN, or END. 
        
        Use EDITOR when you have a story to review. 
        Use HUMAN when you need more information.
        """;
    
    
}