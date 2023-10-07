import { getCountryName } from "./countries";

type Props = {
  countryCode: string;
};

export function CountryName(props: Props) {
  return <>{getCountryName(props.countryCode) || "Unknown"}</>;
}
