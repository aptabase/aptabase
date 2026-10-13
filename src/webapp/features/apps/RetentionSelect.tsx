import { useId } from "react";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@components/Select";
import type { RetentionMonths } from "./apps";

type Props = {
  value: RetentionMonths;
  onChange: (value: RetentionMonths) => void;
  disabled?: boolean;
  creating?: boolean;
};

export function RetentionSelect({ value, onChange, disabled, creating }: Props) {
  const id = useId();

  return (
    <div>
      <label htmlFor={id} className="text-sm mb-2 block font-medium leading-none">
        Event retention
      </label>
      <Select value={String(value)} onValueChange={(value) => onChange(Number(value) as RetentionMonths)} disabled={disabled}>
        <SelectTrigger id={id} aria-describedby={`${id}-description`}>
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="60">5 years</SelectItem>
          <SelectItem value="24">2 years</SelectItem>
          <SelectItem value="12">1 year</SelectItem>
          <SelectItem value="6">6 months</SelectItem>
        </SelectContent>
      </Select>
      <p id={`${id}-description`} className="mt-1 text-sm text-muted-foreground">
        {creating
          ? "New events expire after this period. You can change it later."
          : "Changes apply only to new events. Existing events keep their original expiry."}
        {" "}Debug events are kept for up to 6 months.
      </p>
    </div>
  );
}
