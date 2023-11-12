import { useSearchParams } from "react-router-dom";
import { TopNDataContainer } from "./TopNDataContainer";
import { topAppBuildNumbers, topAppVersions } from "../query";
import { TopNChart } from "./TopNChart";
import { TopNTitle } from "./TopNTitle";

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
          defaultFormat="percentage"
          valueLabel="Sessions"
        />
      )}
    </TopNDataContainer>
  );
}
