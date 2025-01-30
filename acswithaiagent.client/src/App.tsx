import {useEffect, useRef, useState} from "react";
import * as signalr from "@microsoft/signalr";
import {HubConnection} from "@microsoft/signalr";
import Story from './Story.tsx'
import Chat from "./Chat.tsx";
import AgentChitChat from "./AgentChitChat.tsx";
import AgentCurrentPrompts from "./AgentCurrentPrompts.tsx";

export type AgentChitChat = {
    from: string,
    to: string,
    message: string
}

function App() {

    console.log('App repainting')
    const [connection, setConnection] = useState<HubConnection | null>(null)
    const [story, setStory] = useState('')
    const [msgs, setMsgs] = useState<string[]>([])
    const [agentChitChat, setAgentChitChat] = useState<AgentChitChat[]>([])
    const [agentPrompts, setAgentPrompts] = useState<Record<string, string[]>>({})

    const msgsState = useRef<string[]>()
    msgsState.current = msgs

    const agentChitChatState = useRef<AgentChitChat[]>()
    agentChitChatState.current = agentChitChat

    const agentPromptsState = useRef<Record<string, string[]>>()
    agentPromptsState.current = agentPrompts

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
                newConnection.on('AskQuestion', (from: string, question: string, _: string) => {
                    const newState = msgsState.current!.concat([`${from}: ${question}`])
                    setMsgs(newState)
                })
                newConnection.on('ReceiveStoryMessage', (story: string) => {
                    setStory(story)
                })
                newConnection.on('InternalAgentChitChat', (from: string, to: string, message: string) => {
                    const newState = agentChitChatState.current!.concat([{from, to, message}])
                    setAgentChitChat(newState)
                })
                newConnection.on('InternalAgentPrompt', (agent: string, prompt: string[]) => {
                    setAgentPrompts(prevState => ({
                        ...prevState,
                        [agent]: prompt
                    }));
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
        <div className={""}>
            <div className={"row"}>
                <div className={"col"}>
                    <Chat connection={connection} msgs={msgs}/>
                </div>
                <div className={"col"}>
                    <Story story={story}/>
                </div>
                <div className={"col"}>
                    <AgentChitChat agentChitChat={agentChitChat}/>
                </div>
            </div>
            <div className={"row"}>
                <AgentCurrentPrompts agentPrompts={agentPrompts}/>
            </div>
        </div>
    );
}

export default App;
