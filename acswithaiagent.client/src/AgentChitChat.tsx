import {AgentChitChat} from './App'

export type AgentChitChatProps = {
    agentChitChat: AgentChitChat[]
}
export default function ({agentChitChat}: AgentChitChatProps) {
    
    const lengthToShow = 300

    return (
        <div>
            <h3 className={'title'}>Agent Chit Chat</h3>
            <ul className={""}>
                {agentChitChat.map((ac, i) =>
                    <li className={"my-3"} key={i}>{ac.from} to {ac.to}: {ac.message.length > lengthToShow ? `${ac.message.substring(0, lengthToShow)}...` : ac.message}</li>
                )}
            </ul>
        </div>
    )
}
