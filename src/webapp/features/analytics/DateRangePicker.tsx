import { useDatePicker } from "@hooks/use-datepicker";
import { DateRangePickerComponent } from "@components/DateRangePicker/date-range-picker";

export function DateRangePicker() {
  const [period, setPeriod] = useDatePicker();

  return (
    <DateRangePickerComponent
      onUpdate={(values) =>
        setPeriod({
          from: values.range.from.toISOString(),
          to: values.range.to?.toISOString() ?? new Date().toISOString(),
        })
      }
      initialDateFrom={period.from}
      initialDateTo={period.to}
      showCompare={false}
    />
  );
}
