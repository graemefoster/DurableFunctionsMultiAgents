import {AgentChitChat} from './App'

export type AgentChitChatProps = {
    agentChitChat: AgentChitChat[]
}
export default function ({agentChitChat}: AgentChitChatProps) {
    
    const lengthToShow = 300

    return (
        <div>
            <ul className={""}>
                {agentChitChat.map((ac, i) =>
                    <li className={"my-3 box"} key={i}>{ac.from} to {ac.to}: {ac.message.length > lengthToShow ? `${ac.message.substring(0, lengthToShow)}...` : ac.message}</li>
                )}
            </ul>
        </div>
    )
}
