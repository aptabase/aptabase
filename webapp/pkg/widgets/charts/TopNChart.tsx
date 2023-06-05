import { EmptyState, ErrorState } from "@app/primitives";
import { Link } from "react-router-dom";
import { TopNSkeleton } from "./TopNSkeleton";
import { CardTitle } from "../CardTitle";

type Item = {
  name: string;
  value: number;
};

type Props = {
  title: string | JSX.Element;
  labels: [string, string];
  items: Item[];
  renderKeyLabel?: React.ReactNode;
  renderValueLabel?: React.ReactNode;
  renderRow?: (item: Item) => React.ReactNode;
  isLoading?: boolean;
  isError?: boolean;
  searchParamKey?: string;
};

const defaultRenderRow = (item: Item) => (
  <span className="px-2">{item.name || <i>Empty</i>}</span>
);

export function TopNChart(props: Props) {
  const total = props.items.reduce((acc, item) => acc + item.value, 0);
  const renderRow = props.renderRow ?? defaultRenderRow;

  const content = props.isError ? (
    <ErrorState />
  ) : props.isLoading ? (
    <TopNSkeleton />
  ) : props.items.length === 0 ? (
    <EmptyState />
  ) : (
    <>
      <div className="flex w-full flex-row justify-between items-center">
        <div className="text-subtle text-sm font-normal">
          {props.renderKeyLabel ? props.renderKeyLabel : props.labels[0]}
        </div>
        <div className="text-subtle text-sm font-normal pr-1">
          {props.renderValueLabel ? props.renderValueLabel : props.labels[1]}
        </div>
      </div>
      <div className="grid text-sm mt-2 max-h-[22rem] overflow-y-auto">
        {props.items.map((item) => (
          <TopNRow
            key={item.name}
            item={item}
            percentage={item.value / total}
            searchParamKey={props.searchParamKey}
          >
            {renderRow(item)}
          </TopNRow>
        ))}
      </div>
    </>
  );

  return (
    <>
      {typeof props.title === "string" ? (
        <CardTitle>{props.title}</CardTitle>
      ) : (
        props.title
      )}
      {content}
    </>
  );
}

type TopNRowProps = {
  item: Item;
  percentage: number;
  children: React.ReactNode;
  searchParamKey?: string;
};

function TopNRow(props: TopNRowProps) {
  const targetUrl = new URL(window.location.href);
  if (props.searchParamKey) {
    targetUrl.searchParams.set(props.searchParamKey, props.item.name);
  }

  const content = (
    <div className="flex items-center justify-between group py-2 relative">
      <div className="hidden group-hover:block group-hover:bg-subtle absolute h-8 origin-left rounded w-full" />
      <div className="relative z-10 flex w-full max-w-[calc(100%-3rem)] items-center">
        <div
          className="absolute h-8 origin-left bg-primary-100 dark:bg-primary-950 rounded transition-all"
          style={{ width: `${props.percentage * 100}%` }}
        />
        <div className="flex z-10">{props.children}</div>
      </div>
      <p className="text-sm pr-2 z-10">{props.item.value}</p>
    </div>
  );

  if (targetUrl.toString() !== window.location.href) {
    return (
      <Link to={targetUrl.toString()} preventScrollReset={true}>
        {content}
      </Link>
    );
  }

  return content;
}
