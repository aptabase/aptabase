import { CountryFlag, CountryName } from "@features/geo";

type Props = {
  countryCode: string;
};

export function Country(props: Props) {
  return (
    <span className="flex items-center space-x-2">
      <CountryFlag countryCode={props.countryCode} />
      <p>
        <CountryName countryCode={props.countryCode} />
      </p>
    </span>
  );
}
