import { getCountryFlagUrl } from "./countries";

type Props = {
  countryCode: string;
};

export function CountryFlag(props: Props) {
  return <img src={getCountryFlagUrl(props.countryCode)} loading="lazy" className="h-5 w-5 shadow rounded-full" />;
}
