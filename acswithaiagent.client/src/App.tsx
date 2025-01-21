import {useEffect, useRef, useState} from "react";
import * as signalr from "@microsoft/signalr";
import {HubConnection} from "@microsoft/signalr";
import Markdown from "react-markdown";

function App() {

    console.log('App repainting')
    const [connection, setConnection] = useState<HubConnection | null>(null)
    const [story, setStory] = useState('')
    const [msg, setMsg] = useState('')
    const [questionId, setQuestionId] = useState<string | null>()
    const [msgs, setMsgs] = useState<string[]>([])
    const msgsState = useRef<string[]>()
    msgsState.current = msgs

    useEffect(() => {

        const newConnection = new signalr.HubConnectionBuilder()
            .withUrl('/signalr/agenthub')
            .withAutomaticReconnect()
            .build();

        async function connect() {

            console.log('connecting');
            try {
                await newConnection.start()
                setConnection(newConnection)
                newConnection.on('ReceiveMessage', (user: string, message: string) => {
                    const newState = msgsState.current!.concat([`${user}: ${message}`]) 
                    setMsgs(newState)
                })
                newConnection.on('AskQuestion', (from: string, question: string, eventName: string) => {
                    const newState = msgsState.current!.concat([`${from}: ${question}`])
                    setMsgs(newState)
                    setQuestionId(eventName)
                })
                newConnection.on('ReceiveStoryMessage', (story: string) => {
                    setStory(story)
                })
            } catch (e) {
                console.log(e)
            }

        }

        connect().then(() => console.log('connected'))

        return () => {
            console.log('disconnecting')
            if (newConnection.state === signalr.HubConnectionState.Connected) {
                newConnection.stop()
            }
        }

    }, [])

    return (
        <div>
            <h3>The story so far...</h3>
            <Markdown>
                {story || '### No story yet! Press Start Chat to begin'}
            </Markdown>
            {
                (connection?.state === signalr.HubConnectionState.Connected) && <div>
                    <button onClick={() => {
                        connection!.invoke('NewChat')
                            .then(() => console.log('message sent'))
                            .catch(e => console.log(e))
                    }}>Start Chat
                    </button>
                    <h3>Connected</h3>
                    <ul>
                        {msgs.map((m, i) => <li key={i}>{m}</li>)}
                    </ul>
                    <input type="text" onChange={e => setMsg(e.target.value)}/>
                    <button onClick={() => {
                        connection!.invoke('UserResponse', msg, questionId)
                            .then(() => console.log('message sent'))
                            .catch(e => console.log(e))
                    }}>Send {questionId}</button>
                </div>
            }
        </div>
    );
}

export default App;
