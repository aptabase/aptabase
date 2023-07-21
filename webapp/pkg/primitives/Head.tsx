import { useEffect } from "react";

type Props = {
  title: string;
};

export function Head(props: Props) {
  useEffect(() => {
    document.title = `${props.title} · Aptabase`;
  }, [props.title]);

  return null;
}
