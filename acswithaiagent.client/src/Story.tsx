import Markdown from "react-markdown";

export type StoryProps = {
    story: string
}

export default function({story}: StoryProps) {
    return (
        <div>
            <h3>The story so far...</h3>
            <Markdown>
                {story}
            </Markdown>
        </div>
    )
}

