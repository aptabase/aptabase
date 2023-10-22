import { useSearchParams } from "react-router-dom";
import { TopNDataContainer } from "./TopNDataContainer";
import { topCountries, topRegions } from "../query";
import { Country } from "./Country";
import { TopNChart } from "./TopNChart";
import { TopNTitle } from "./TopNTitle";

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
            id="regions"
            key="regions"
            title={
              <TopNTitle backProperty="countryCode">
                <Country countryCode={countryCode} />
              </TopNTitle>
            }
            defaultFormat="percentage"
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
          id="countries"
          key="countries"
          title="Countries"
          searchParamKey="countryCode"
          defaultFormat="percentage"
          valueLabel="Sessions"
          renderRow={(item) => <Country countryCode={item.name} />}
        />
      )}
    </TopNDataContainer>
  );
}
