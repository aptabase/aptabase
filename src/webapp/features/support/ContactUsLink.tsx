import { useSupport } from "./useSupport";

type Props = {
  className: string;
  children: React.ReactNode;
};

export function ContactUsLink(props: Props) {
  const { toggleChat } = useSupport();
  return (
    <button className={props.className} onClick={toggleChat} type="button">
      {props.children}
    </button>
  );
}
