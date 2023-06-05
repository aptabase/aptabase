import dart from "highlight.js/lib/languages/dart";
import gradle from "highlight.js/lib/languages/gradle";
import ReactMarkdown from "react-markdown";
import rehypeHighlight from "rehype-highlight";
import remarkGfm from "remark-gfm";

import "./Markdown.css";

type Props = {
  content: string;
};

export function Markdown(props: Props) {
  return (
    <ReactMarkdown
      className="prose dark:prose-invert max-w-none prose-table:max-w-fit prose-img:my-0"
      remarkPlugins={[remarkGfm]}
      rehypePlugins={[
        [
          rehypeHighlight,
          {
            subset: ["rust", "gradle", "dart", "swift"],
            languages: { dart, gradle },
          },
        ],
      ]}
      components={{
        blockquote: ({ node, ...props }) => (
          <div className="border-l-4 border-default pl-2">{props.children}</div>
        ),
        img: ({ node, ...props }) => {
          if (props.src?.endsWith("og.png")) return null;
          return <img {...props} />;
        },
        a: ({ node, ...props }) => (
          <a target="_blank" className="inline-block" {...props} />
        ),
      }}
    >
      {props.content}
    </ReactMarkdown>
  );
}
