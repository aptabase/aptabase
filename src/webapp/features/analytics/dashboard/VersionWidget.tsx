import { TopNDataContainer } from "./TopNDataContainer";
import { topAppVersions } from "../query";
import { TopNChart } from "./TopNChart";

type Props = {
  appId: string;
};

export function VersionWidget(props: Props) {
  return (
    <TopNDataContainer appId={props.appId} queryName="top-appversions" query={topAppVersions}>
      {(data) => (
        <TopNChart
          {...data}
          id="appversions"
          key="appversions"
          title="App Versions"
          searchParamKey="appVersion"
          defaultFormat="percentage"
          valueLabel="Sessions"
        />
      )}
    </TopNDataContainer>
  );
}
