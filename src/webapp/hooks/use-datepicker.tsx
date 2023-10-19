import { useSearchParams } from "react-router-dom";

const setStoredValue = (value: string) => window.localStorage?.setItem("period", value);
const getStoredValue = () => window.localStorage?.getItem("period") ?? "24h";

export function useDatePicker(): [string, (value: string) => void] {
  let [searchParams, setSearchParams] = useSearchParams();
  const period = searchParams.get("period") ?? getStoredValue();

  const setPeriod = (value: string) => {
    setStoredValue(value);
    setSearchParams((params) => {
      params.set("period", value);
      return params;
    });
  };

  return [period, setPeriod];
}
