import { Link } from "react-router-dom";
import { TopNSkeleton } from "./TopNSkeleton";
import { TopNTitle } from "./TopNTitle";
import { EmptyState } from "@components/EmptyState";
import { ErrorState } from "@components/ErrorState";
import { formatNumber } from "@fns/format-number";
import { useLocalStorage } from "@hooks/use-localstorage";
import { twMerge } from "tailwind-merge";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@components/Tooltip";
import { QueryObserverResult, RefetchOptions } from "@tanstack/react-query";

type Item = {
  name: string;
  value: number;
};

type Props = {
  id: string;
  title: string | JSX.Element;
  items: Item[];
  keyLabel?: React.ReactNode;
  valueLabel?: React.ReactNode;
  renderRow?: (item: Item) => React.ReactNode;
  defaultFormat: "absolute" | "percentage";
  isLoading?: boolean;
  isError?: boolean;
  searchParamKey?: string;
  externalLink?: (item: Item) => string;
  refetch?: (options?: RefetchOptions | undefined) => Promise<QueryObserverResult<any, Error>>;
};

const defaultRenderRow = (item: Item) => <>{item.name || <i>Empty</i>}</>;

export function TopNChart(props: Props) {
  const [format, setFormat] = useLocalStorage<"absolute" | "percentage">(
    `top_n_${props.id}_format`,
    props.defaultFormat
  );
  const total = props.items.reduce((acc, item) => acc + item.value, 0);
  const renderRow = props.renderRow ?? defaultRenderRow;

  const canChangeFormat = typeof props.valueLabel === "string";

  const toggleFormat = () => {
    if (!canChangeFormat) return;

    setFormat(format === "absolute" ? "percentage" : "absolute");
  };

  const content = props.isError ? (
    <ErrorState refetch={props.refetch} />
  ) : props.isLoading ? (
    <TopNSkeleton />
  ) : props.items.length === 0 ? (
    <EmptyState />
  ) : (
    <>
      <div className="flex w-full flex-row justify-between items-end">
        <div>
          {typeof props.title === "string" ? <TopNTitle>{props.title}</TopNTitle> : props.title}
          {props.keyLabel && <div className="text-muted-foreground text-sm font-normal">{props.keyLabel}</div>}
        </div>
        <div
          onClick={toggleFormat}
          className={twMerge("text-muted-foreground text-sm font-normal pr-1", canChangeFormat ? "cursor-pointer" : "")}
        >
          {props.valueLabel}
        </div>
      </div>
      <div className="grid text-sm mt-2 max-h-[22rem] overflow-y-auto">
        {props.items.map((item) => (
          <TopNRow
            key={item.name}
            item={item}
            format={format}
            percentage={Math.round(item.value) / total}
            searchParamKey={props.searchParamKey}
            externalLink={props.externalLink?.(item)}
          >
            <div className="px-2">{renderRow(item)}</div>
          </TopNRow>
        ))}
      </div>
    </>
  );

  return content;
}

type TopNRowProps = {
  item: Item;
  percentage: number;
  format: "absolute" | "percentage";
  children: React.ReactNode;
  searchParamKey?: string;
  externalLink?: string;
};

function TopNRow(props: TopNRowProps) {
  let targetUrlStr = "";
  if (props.externalLink) {
    targetUrlStr = props.externalLink;
  } else {
    const targetUrl = new URL(window.location.href);

    targetUrlStr = window.location.href;
    if (props.searchParamKey) {
      if (props.searchParamKey) {
        targetUrl.searchParams.set(props.searchParamKey, props.item.name);
      }
      targetUrlStr = targetUrl.toString();
    }
  }

  const content = (
    <div className="flex items-center justify-between group py-2 relative">
      <div className="hidden group-hover:block group-hover:bg-accent absolute h-8 origin-left rounded w-full" />
      <div className="relative z-10 flex w-full max-w-[calc(100%-3rem)] items-center">
        <div
          className="absolute h-8 origin-left bg-primary-100 dark:bg-primary-900 rounded transition-all"
          style={{ width: `${Math.min(props.percentage, 1) * 100}%` }}
        />
        <div className="flex z-10">{props.children}</div>
      </div>
      <p className="text-sm pr-2 z-10 tabular-nums">
        {props.format === "percentage" ? (
          `${Math.round(props.percentage * 100)}%`
        ) : props.item.value >= 1e3 ? (
          <TooltipProvider delayDuration={0}>
            <Tooltip>
              <TooltipContent>{props.item.value}</TooltipContent>
              <TooltipTrigger>{formatNumber(props.item.value)}</TooltipTrigger>
            </Tooltip>
          </TooltipProvider>
        ) : (
          props.item.value
        )}
      </p>
    </div>
  );

  if (targetUrlStr !== window.location.href) {
    return (
      <Link to={targetUrlStr} preventScrollReset={true}>
        {content}
      </Link>
    );
  }

  return content;
}
