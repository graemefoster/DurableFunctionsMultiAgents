import {AgentMessage} from './App'

export type AgentChitChatProps = {
    agentChitChat: AgentMessage[]
}
export default function ({agentChitChat}: AgentChitChatProps) {
    
    const lengthToShow = 300

    return (
        <div>
            <ul className={""}>
                {agentChitChat.map((ac, i) =>
                    <li className={"my-3 box"} key={i}>{ac.date.toLocaleTimeString()}: {ac.from} to {ac.to}: {ac.message.length > lengthToShow ? `${ac.message.substring(0, lengthToShow)}...` : ac.message}</li>
                )}
            </ul>
        </div>
    )
}
