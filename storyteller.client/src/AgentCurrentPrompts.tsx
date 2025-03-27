import {useState} from "react";
import AgentChitChatView from "./AgentChitChat.tsx";
import {AgentMessage} from './App'

export type AgentsCurrentPromptProps = {
    agentPrompts: Record<string, string[]>
    agentChitChat: AgentMessage[]
}
export default function ({agentPrompts, agentChitChat}: AgentsCurrentPromptProps) {

    const [activeTab, setActiveTab] = useState<number>(999)

    return (
        <div>
            <div className={"tabs is-boxed"}>
                <ul>
                    <li className={activeTab == 999 ? 'is-active' : ''}>
                        <a onClick={() => setActiveTab(999)}>Chit Chat</a>
                    </li>
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
                {activeTab === 999 &&
                    <div id={`nav-999`} key={'999'}>
                        <AgentChitChatView agentChitChat={agentChitChat}/>
                    </div>
                }
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

function AgentCurrentPrompt({prompt}: AgentCurrentPromptProps) {
    return (
        <div>
            <ul className={"list-group"}>
                {prompt.map((message, j) => (
                    <li className={"box"} key={j}>
                        {message.split('\n').map((line: string, idx: number) => (<p key={idx}>{line}<br/></p>))}
                    </li>
                ))}
            </ul>
        </div>)

}
