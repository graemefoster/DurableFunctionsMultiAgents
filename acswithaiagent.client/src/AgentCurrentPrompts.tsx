import {useState} from "react";

export type AgentsCurrentPromptProps = {
    agentPrompts: Record<string, string[]>
}
export default function ({agentPrompts}: AgentsCurrentPromptProps) {

    const [activeTab, setActiveTab] = useState<number>(0)

    return (
        <div>
            <div className={"tabs is-boxed"}>
                <ul>
                    {
                        Object.keys(agentPrompts).map((agent, i) => (
                            <li className={i == activeTab ? 'is-active' : ''} key={i}>
                                <a onClick={() => setActiveTab(i)}>{agent}</a>
                            </li>
                        ))
                    }
                </ul>
            </div>
            <div className={"tab-content"}>
                {
                    Object.keys(agentPrompts).map((agent, i) => (
                        i === activeTab &&
                        <div id={`nav-${agent}`} key={i}>
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
            <ul className={"list-group"}>
                {prompt.map((message, j) => (
                    <li className={"box"} key={j}>
                        {message.replace('\n', '<br/>')}
                    </li>
                ))}
            </ul>
        </div>)

}
