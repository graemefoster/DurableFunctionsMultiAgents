import {
    headingsPlugin,
    listsPlugin,
    markdownShortcutPlugin,
    MDXEditor, MDXEditorMethods,
    quotePlugin,
    thematicBreakPlugin
} from '@mdxeditor/editor'
import '@mdxeditor/editor/style.css'
import {useEffect, useRef} from "react";

export type StoryProps = {
    story: string,
    onStoryEdit: (updatedStory: string) => void 
}

export default function ({ story, onStoryEdit}: StoryProps) {

    const ref = useRef<MDXEditorMethods>(null)
    useEffect(() => {
        ref.current?.setMarkdown(story)
    }, [story]);

    return (
        <div>
            <h3>The story so far...</h3>
            
            <MDXEditor markdown={story} 
                       onChange={md => onStoryEdit(md)}
                       ref={ref}
                       plugins={[
                           headingsPlugin(),
                           listsPlugin(),
                           quotePlugin(),
                           thematicBreakPlugin(),
                           markdownShortcutPlugin()
                       ]}
            />
        </div>
    )
}

