import { ToggleGroup, ToggleGroupList, ToggleGroupTrigger } from "@components/ToggleGroup";

type Props = {
  value: string;
  onChange: (value: string) => void;
};

export function FormatPicker(props: Props) {
  return (
    <div className="flex flex-col text-xs max-w-md">
      <p className="font-medium mb-1">Export Format</p>
      <ToggleGroup defaultValue="csv" onValueChange={props.onChange}>
        <ToggleGroupList>
          <ToggleGroupTrigger value="csv" className="text-xs">
            CSV
          </ToggleGroupTrigger>
          <ToggleGroupTrigger value="parquet" className="text-xs">
            Parquet
          </ToggleGroupTrigger>
        </ToggleGroupList>
      </ToggleGroup>
      {props.value === "csv" && (
        <span className="text-muted-foreground p-1">
          CSV format has better compatibility with most tools, but results in larger files and may be limited depending
          on how much data you have.
        </span>
      )}
      {props.value === "parquet" && (
        <span className="text-muted-foreground p-1">
          Parquet has much better compression and is more efficient for large datasets, but may need to be converted to
          other format before using it with some tools.
        </span>
      )}
    </div>
  );
}
