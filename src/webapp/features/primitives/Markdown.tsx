import dart from "highlight.js/lib/languages/dart";
import gradle from "highlight.js/lib/languages/gradle";
import ReactMarkdown from "react-markdown";
import rehypeHighlight from "rehype-highlight";
import remarkGfm from "remark-gfm";
import rehypeRaw from "rehype-raw";

import "./Markdown.css";

type Props = {
  content: string;
  baseURL: string;
};

export function Markdown(props: Props) {
  return (
    <ReactMarkdown
      className="prose dark:prose-invert max-w-none prose-table:max-w-fit prose-img:my-0"
      remarkPlugins={[remarkGfm]}
      rehypePlugins={[
        rehypeRaw,
        [
          rehypeHighlight,
          {
            subset: ["rust", "gradle", "dart", "swift"],
            languages: { dart, gradle },
          },
        ],
      ]}
      components={{
        blockquote: ({ node, ...props }) => <div className="border-l-4 pl-2">{props.children}</div>,
        img: ({ node, ...imgProps }) => {
          if (imgProps.src?.endsWith("og.png")) return null;
          const src = imgProps.src?.startsWith("http")
            ? imgProps.src
            : `${props.baseURL}/${imgProps.src}`;

          return <img {...imgProps} src={src} />;
        },
        a: ({ node, ...props }) => <a target="_blank" className="inline-block" {...props} />,
      }}
    >
      {props.content}
    </ReactMarkdown>
  );
}
