import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@components/Select";
import { useAtom } from "jotai";
import { periodAtom } from "../../../atoms/date-atoms";

type Option = {
  value: string;
  name: string;
};

const options: Option[] = [
  { value: "today", name: "Today" },
  { value: "yesterday", name: "Yesterday" },
  { value: "divider-6", name: "Divider" },
  { value: "24h", name: "Last 24 hours" },
  { value: "48h", name: "Last 48 hours" },
  { value: "divider-2", name: "Divider" },
  { value: "7d", name: "Last 7 days" },
  { value: "30d", name: "Last 30 days" },
  { value: "divider-1", name: "Divider" },
  { value: "90d", name: "Last 3 months" },
  { value: "180d", name: "Last 6 months" },
  { value: "365d", name: "Last 12 months" },
  { value: "divider-4", name: "Divider" },
  { value: "all", name: "All time" },
  { value: "custom", name: "Custom" },
];

type StyledOptionProps = {
  option: Option;
};

function Item(props: StyledOptionProps) {
  if (props.option.name === "Divider") {
    return <div className="border-t my-1" />;
  }

  return (
    <SelectItem key={props.option.value} value={props.option.value}>
      {props.option.name}
    </SelectItem>
  );
}

export function DateRangePicker() {
  const [period, setPeriod] = useAtom(periodAtom);

  return (
    <Select value={period} onValueChange={setPeriod}>
      <SelectTrigger className="w-36">
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        {options.map((option) => (
          <Item key={option.value} option={option} />
        ))}
      </SelectContent>
    </Select>
  );
}
