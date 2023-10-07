import { getCountryFlagUrl } from "./countries";

type Props = {
  countryCode: string;
  size?: "sm" | "" | "md";
};

const sizeClasses = {
  sm: "h-4 w-4",
  "": "h-5 w-5",
  md: "h-6 w-6",
};

export function CountryFlag(props: Props) {
  const sizeClass = sizeClasses[props.size ?? ""];

  return (
    <img src={getCountryFlagUrl(props.countryCode)} loading="lazy" className={`${sizeClass} shadow rounded-full`} />
  );
}
