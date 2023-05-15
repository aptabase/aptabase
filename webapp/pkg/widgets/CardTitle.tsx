import { IconArrowBackUp } from "@tabler/icons-react";
import { Link } from "react-router-dom";

type Props = {
  backProperty?: string;
  children: React.ReactNode;
};

export function CardTitle(props: Props) {
  if (!props.backProperty) {
    return <>{props.children}</>;
  }

  const targetUrl = new URL(window.location.href);
  targetUrl.searchParams.delete(props.backProperty);

  return (
    <span className="inline-flex items-center relative group">
      <div className="group-hover:blur-sm group-hover:opacity-50 transition-all">
        {props.children}
      </div>
      {props.backProperty && (
        <Link
          to={targetUrl.toString()}
          className="text-sm items-center space-x-2 absolute m-2 hidden group-hover:flex transition-all"
          preventScrollReset={true}
        >
          <IconArrowBackUp strokeWidth={1.75} className="h-5 w-5" />{" "}
          <span>Back</span>
        </Link>
      )}
    </span>
  );
}
