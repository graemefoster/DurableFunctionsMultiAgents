import {useEffect, useRef, useState} from "react";
import * as signalr from "@microsoft/signalr";
import {HubConnection} from "@microsoft/signalr";
import Story from './Story.tsx'
import Chat from "./Chat.tsx";
import AgentChitChat from "./AgentChitChat.tsx";
import AgentCurrentPrompts from "./AgentCurrentPrompts.tsx";
import {MDXEditor, MDXEditorMethods} from "@mdxeditor/editor";

export type AgentChitChat = {
    from: string,
    to: string,
    message: string
}

function App() {

    console.log('App repainting')
    const [connection, setConnection] = useState<HubConnection | null>(null)
    const [previousStory, setPreviousStory] = useState('')
    const [story, setStory] = useState('Story will appear here')
    const [updatedStory, setUpdatedStory] = useState('Story will appear here')
    const [msgs, setMsgs] = useState<string[]>([])
    const [agentChitChat, setAgentChitChat] = useState<AgentChitChat[]>([])
    const [agentPrompts, setAgentPrompts] = useState<Record<string, string[]>>({})

    const storyState = useRef<string>()
    storyState.current = story

    const msgsState = useRef<string[]>()
    msgsState.current = msgs

    const agentChitChatState = useRef<AgentChitChat[]>()
    agentChitChatState.current = agentChitChat

    const agentPromptsState = useRef<Record<string, string[]>>()
    agentPromptsState.current = agentPrompts

    const ref = useRef<MDXEditorMethods>(null)
    useEffect(() => {
        ref.current?.setMarkdown(previousStory)
    }, [previousStory]);


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
                newConnection.on('ReceiveStoryMessage', (newStory: string) => {
                    setPreviousStory(storyState.current!)
                    setStory(newStory)
                    setUpdatedStory(newStory)
                    console.log('Story received', newStory)
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
        <div className={'m-5'}>
            <div className={"columns"}>
                <div className={"column"}>
                    <Chat connection={connection} msgs={msgs} story={story} updatedStory={updatedStory}/>
                </div>
                <div className={"column"}>
                    <div>
                        <h3 className={'title'}>Previous story</h3>
                        <MDXEditor ref={ref} markdown={previousStory}/>
                    </div>
                </div>
                <div className={"column"}>
                    <Story story={updatedStory} onStoryEdit={(updatedStory) => {
                        setUpdatedStory(updatedStory);
                        console.log('Story changed')
                    }}/>
                </div>
                <div className={"column"}>
                    <AgentChitChat agentChitChat={agentChitChat}/>
                </div>
            </div>
            <div>
                <AgentCurrentPrompts agentPrompts={agentPrompts}/>
            </div>
        </div>
    );
}

export default App;
