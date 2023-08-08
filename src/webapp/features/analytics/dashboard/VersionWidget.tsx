import { useSearchParams } from "react-router-dom";
import { TopNDataContainer } from "./TopNDataContainer";
import { topAppBuildNumbers, topAppVersions } from "../query";
import { TopNChart, TopNTitle } from "@features/primitives";

type Props = {
  appId: string;
};

export function VersionWidget(props: Props) {
  const [searchParams] = useSearchParams();
  const appVersion = searchParams.get("appVersion") || "";

  if (appVersion) {
    return (
      <TopNDataContainer
        appId={props.appId}
        queryName="top-appbuildnumbers"
        query={topAppBuildNumbers}
      >
        {(data) => (
          <TopNChart
            {...data}
            title={<TopNTitle backProperty="appVersion">{appVersion}</TopNTitle>}
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
          title="App Versions"
          searchParamKey="appVersion"
          valueLabel="Sessions"
        />
      )}
    </TopNDataContainer>
  );
}
