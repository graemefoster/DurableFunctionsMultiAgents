import * as signalr from "@microsoft/signalr";
import {HubConnection} from "@microsoft/signalr";
import {useEffect, useState} from "react";

export type ChatProps = {
    connection: HubConnection | null
    msgs: string[]
}

export default function ({connection, msgs}: ChatProps) {

    const [msg, setMsg] = useState('')
    const [questionId, setQuestionId] = useState<string | null>(null)
    const [isChatting, setIsChatting] = useState(false)

    useEffect(() => {

            if (connection !== null) {
                connection.on('AskQuestion', (_1: string, _2: string, eventName: string) => {
                    setQuestionId(eventName)
                })
            }
        },
        [connection])

    if (connection?.state !== signalr.HubConnectionState.Connected) {
        return <div>Connecting...</div>
    }

    const question = questionId !== null && <div>
        <input type="text" onChange={e => setMsg(e.target.value)}/>
        <button onClick={() => {
            connection!.invoke('UserResponse', msg, questionId)
                .then(() => console.log('message sent'))
                .then(() => setQuestionId(null))
                .catch(e => console.log(e))
        }}>Reply
        </button>
    </div>

    const newChat = !isChatting && <button onClick={() => {
        connection!.invoke('NewChat')
            .then(() => console.log('message sent'))
            .then(() => setIsChatting(true))
            .catch(e => console.log(e))
    }}>Start New Story
    </button>

    const messageList = (<ul className={"list-group"}>
        {msgs.map((m, i) => <li className={"list-group-item"} key={i}>{m}</li>)}
    </ul>)

    return <div>
        {newChat}
        <h3>Story Chat</h3>
        {messageList}
        {question}
    </div>
}