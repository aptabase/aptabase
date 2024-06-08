import { useSearchParams } from "react-router-dom";

type DateRange = { from: string; to: string };
const setStoredValue = (value: DateRange) => {
  window.localStorage?.setItem("from", value.from);
  window.localStorage?.setItem("to", value.to);
};
const getStoredValue = () => {
  const startOfDay = new Date();
  startOfDay.setHours(0, 0, 0, 0);

  const endOfDay = new Date();
  endOfDay.setHours(11, 59, 59, 0);
  const from = window.localStorage?.getItem("from") ?? startOfDay.toISOString();

  const to = window.localStorage?.getItem("to") ?? endOfDay.toISOString();

  return { from, to };
};

export function useDatePicker(): [DateRange, (value: DateRange) => void] {
  let [searchParams, setSearchParams] = useSearchParams();
  const period = {
    from: searchParams.get("from") ?? getStoredValue().from,
    to: searchParams.get("to") ?? getStoredValue().to,
  };

  const setPeriod = (value: DateRange) => {
    setStoredValue(value);
    setSearchParams((params) => {
      params.set("from", value.from);
      params.set("to", value.to);
      return params;
    });
  };

  return [period, setPeriod];
}
