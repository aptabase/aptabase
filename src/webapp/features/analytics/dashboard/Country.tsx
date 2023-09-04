import { getCountryFlagUrl, getCountryName } from "./countries";

type Props = {
  countryCode: string;
};

export function Country(props: Props) {
  return (
    <span className="flex items-center space-x-2">
      <img
        src={getCountryFlagUrl(props.countryCode)}
        loading="lazy"
        className="h-5 w-5 shadow rounded-full"
      />
      <p>{getCountryName(props.countryCode) || "Unknown"}</p>
    </span>
  );
}
