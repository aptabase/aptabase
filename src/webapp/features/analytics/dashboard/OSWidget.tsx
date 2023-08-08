import { useSearchParams } from "react-router-dom";
import { TopNDataContainer } from "./TopNDataContainer";
import { topOSVersions, topOperatingSystem } from "../query";
import { OS } from "./OS";
import { TopNChart, TopNTitle } from "@features/primitives";

type Props = {
  appId: string;
};

export function OSWidget(props: Props) {
  const [searchParams] = useSearchParams();
  const osName = searchParams.get("osName") || "";

  if (osName) {
    return (
      <TopNDataContainer appId={props.appId} queryName="top-osversions" query={topOSVersions}>
        {(data) => (
          <TopNChart
            {...data}
            title={
              <TopNTitle backProperty="osName">
                <OS name={osName} />
              </TopNTitle>
            }
            valueLabel="Sessions"
          />
        )}
      </TopNDataContainer>
    );
  }

  return (
    <TopNDataContainer
      appId={props.appId}
      queryName="top-operatingsystems"
      query={topOperatingSystem}
    >
      {(data) => (
        <TopNChart
          {...data}
          title="Operating Systems"
          searchParamKey="osName"
          valueLabel="Sessions"
          renderRow={(item) => <OS name={item.name} />}
        />
      )}
    </TopNDataContainer>
  );
}
