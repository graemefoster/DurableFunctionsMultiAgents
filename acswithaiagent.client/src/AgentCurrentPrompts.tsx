export type AgentsCurrentPromptProps = {
    agentPrompts: Record<string, string[]>
}
export default function ({agentPrompts}: AgentsCurrentPromptProps) {

    return (
        <div>
            <div className={"nav nav-tabs"}>
                {
                    Object.keys(agentPrompts).map((agent, i) => (
                        <button className={"nav-link"} id={`nav-${agent}-tab`} data-bs-toggle={"tab"} data-bs-target={`#nav-${agent}`} type={"button"} role={"tab"} aria-controls={`nav-${agent}`} aria-selected={"true"} key={i}>{agent}</button>
                    ))
                }
            </div>
            <div className={"tab-content"}>
                {
                    Object.keys(agentPrompts).map((agent, i) => (
                        <div className={"tab-pane fade show"} id={`nav-${agent}`} role={"tabpanel"} aria-labelledby={`nav-${agent}-tab`} key={i}>
                            <AgentCurrentPrompt agent={agent} prompt={agentPrompts[agent]}/>
                        </div>
                    ))
                }
            </div>
        </div>
    )
}

type AgentCurrentPromptProps = {
    agent: string
    prompt: string[]
}

function AgentCurrentPrompt({agent, prompt}: AgentCurrentPromptProps) {
    return (
        <div>
            <strong>{agent}:</strong>
            <ul className={"list-group"}>
                {prompt.map((message, j) => (
                    <li className={"list-group-item"} key={j}>
                        <pre>{message.replace('\n', '<br/>')}</pre>
                    </li>
                ))}
            </ul>
        </div>)

}
