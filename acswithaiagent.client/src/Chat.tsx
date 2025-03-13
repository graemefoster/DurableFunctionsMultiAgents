import * as signalr from "@microsoft/signalr";
import {HubConnection} from "@microsoft/signalr";
import {useEffect, useState} from "react";
import {diffWords} from "diff"

export type ChatProps = {
    connection: HubConnection | null
    msgs: string[]
    story: string
    updatedStory: string
}

function generateChangeMessages(oldStr: string, newStr: string): string[] {
    const changes = diffWords(oldStr, newStr, {
        ignoreCase: true,
        ignoreWhitespace: true,
    });
    const messages: string[] = [];
    console.log(changes)

    changes.forEach((change, _) => {
        if (!change.added && !change.removed) {
            messages.push(`Then ${change.count} words were untouched`)
        }
        if (change.removed) {
            messages.push(`Then ${change.count} word${(change.count! > 1 ? 's' : '')}: '${change.value.trim()}' was removed`);
        }
        if (change.added) {
            messages.push(`Then ${change.count} word${(change.count! > 1 ? 's' : '')}: '${change.value.trim()}' was added`);
        }
    });

    return messages;
}


export default function ({connection, msgs, story, updatedStory}: ChatProps) {

    const [msg, setMsg] = useState('')
    const [questionId, setQuestionId] = useState<string | null>(null)
    const [targetAgent, setTargetAgent ] = useState<string | null>(null)
    const [isChatting, setIsChatting] = useState(false)

    useEffect(() => {

            if (connection !== null) {
                connection.on('AskQuestion', (fromAgent: string, _: string, eventName: string) => {
                    setQuestionId(eventName)
                    setTargetAgent(fromAgent)
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
            connection!.invoke('UserResponse', targetAgent, msg, questionId)
                .then(() => console.log('message sent'))
                .then(() => setQuestionId(null))
                .then(() => setTargetAgent(null))
                .catch(e => console.log(e))
        }}>Reply
        </button>
    </div>

    const updateButton = story === updatedStory ? <div/> : <div>
        <button onClick={() => {
            const storyPatch = generateChangeMessages(story, updatedStory)
            console.log('Patch:: ', storyPatch)
            const storyPatchString = storyPatch.reduce((acc, curr) => acc + curr + '\n', '')
            connection!.invoke('DiffResponse', storyPatchString, questionId)
                .then(() => console.log('User edit sent'))
                .then(() => setQuestionId(null))
                .catch(e => console.log(e))
        }}>Update from edits
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
        {updateButton}
    </div>
}