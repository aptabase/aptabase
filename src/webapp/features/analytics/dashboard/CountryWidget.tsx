import { useSearchParams } from "react-router-dom";
import { TopNDataContainer } from "./TopNDataContainer";
import { topCountries, topRegions } from "../query";
import { Country } from "./Country";
import { TopNChart, TopNTitle } from "@features/primitives";

type Props = {
  appId: string;
};

export function CountryWidget(props: Props) {
  const [searchParams] = useSearchParams();
  const countryCode = searchParams.get("countryCode") || "";

  if (countryCode) {
    return (
      <TopNDataContainer appId={props.appId} queryName="top-regions" query={topRegions}>
        {(data) => (
          <TopNChart
            {...data}
            title={
              <TopNTitle backProperty="countryCode">
                <Country countryCode={countryCode} />
              </TopNTitle>
            }
            valueLabel="Sessions"
          />
        )}
      </TopNDataContainer>
    );
  }

  return (
    <TopNDataContainer appId={props.appId} queryName="top-countries" query={topCountries}>
      {(data) => (
        <TopNChart
          {...data}
          title="Countries"
          searchParamKey="countryCode"
          valueLabel="Sessions"
          renderRow={(item) => <Country countryCode={item.name} />}
        />
      )}
    </TopNDataContainer>
  );
}
