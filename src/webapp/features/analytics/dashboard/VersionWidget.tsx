import { useSearchParams } from "react-router-dom";
import { TopNDataContainer } from "./TopNDataContainer";
import { topAppBuildNumbers, topAppVersions } from "../query";
import { TopNChart } from "./TopNChart";
import { TopNTitle } from "./TopNTitle";

type Props = {
  appId: string;
};

export function VersionWidget(props: Props) {
  const [searchParams] = useSearchParams();
  const appVersion = searchParams.get("appVersion") || "";

  if (appVersion) {
    return (
      <TopNDataContainer appId={props.appId} queryName="top-appbuildnumbers" query={topAppBuildNumbers}>
        {(data) => (
          <TopNChart
            {...data}
            id="appbuildnumbers"
            key="appbuildnumbers"
            title={<TopNTitle backProperty="appVersion">{appVersion}</TopNTitle>}
            defaultFormat="percentage"
            valueLabel="Sessions"
          />
        )}
      </TopNDataContainer>
    );
  }

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
