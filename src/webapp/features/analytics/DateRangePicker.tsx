import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@components/Select";
import { useDatePicker } from "@hooks/use-datepicker";

type Option = {
  value: string;
  name: string;
};

const options: Option[] = [
  { value: "24h", name: "Last 24 hours" },
  { value: "48h", name: "Last 48 hours" },
  { value: "divider-2", name: "Divider" },
  { value: "7d", name: "Last 7 days" },
  { value: "14d", name: "Last 14 days" },
  { value: "30d", name: "Last 30 days" },
  { value: "divider-1", name: "Divider" },
  { value: "month", name: "This month" },
  { value: "last-month", name: "Last month" },
  { value: "divider-3", name: "Divider" },
  { value: "90d", name: "Last 3 months" },
  { value: "180d", name: "Last 6 months" },
  { value: "365d", name: "Last 12 months" },
  { value: "divider-4", name: "Divider" },
  { value: "all", name: "All time" },
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
  const [period, setPeriod] = useDatePicker();

  return (
    <Select value={period} onValueChange={setPeriod}>
      <SelectTrigger className="w-36">
        <SelectValue placeholder="Theme" />
      </SelectTrigger>
      <SelectContent>
        {options.map((option) => (
          <Item key={option.value} option={option} />
        ))}
      </SelectContent>
    </Select>
  );
}
