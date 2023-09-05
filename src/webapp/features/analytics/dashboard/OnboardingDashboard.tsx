import { TopNChart } from "@features/primitives";
import { Application, getAppById } from "@features/apps";
import { useQuery } from "@tanstack/react-query";
import { WaitingForEventsPopup } from "./WaitingForEventsPopup";
import { MetricsChart } from "../key_metrics/MetricsChart";
import { Country } from "./Country";
import { OS } from "./OS";
import { Metric } from "../key_metrics/Metric";
import { KeyMetricsContainer } from "../key_metrics/MetricsContainer";
import { Page, PageHeading } from "@components/Page";

type Props = {
  app: Application;
};

export function OnboardingDashboard(props: Props) {
  const { data: hasEvents } = useQuery(
    ["app-onboarding", props.app.id],
    async () => {
      const app = await getAppById(props.app.id);
      return app.hasEvents;
    },
    { refetchInterval: 5000 }
  );

  if (hasEvents) {
    location.reload();
    return null;
  }

  const containerClassName = "min-h-[12rem] bg-background py-4 sm:px-4";

  const usersPerHour = [
    10, 30, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 40, 30, 20, 10, 5, 10, 15, 20, 25, 30, 35, 40,
  ];

  return (
    <Page title={props.app.name}>
      <div className="space-y-4">
        <div className="flex justify-between items-center">
          <PageHeading title="Dashboard" />
        </div>
        <div className="relative bg-background pt-2">
          <WaitingForEventsPopup appId={props.app.id} />

          <KeyMetricsContainer>
            <Metric
              label="Daily Users"
              current={42}
              previous={24}
              activeClassName="bg-primary"
              active={true}
              format="number"
            />
            <Metric
              label="Sessions"
              current={94}
              previous={65}
              activeClassName="bg-primary"
              format="number"
            />
            <Metric
              label="Events"
              activeClassName="bg-foreground"
              current={3504}
              previous={2406}
              format="number"
            />
            <Metric label="Avg. Duration" current={340} format="duration" />
          </KeyMetricsContainer>

          <MetricsChart
            isEmpty={false}
            activeMetric="users"
            showEvents={false}
            isError={false}
            isLoading={false}
            hasPartialData={true}
            users={[
              10, 30, 15, 25, 26, 28, 24, 32, 40, 50, 42, 30, 40, 24, 50, 40, 42, 39, 24, 20, 25,
              30, 35, 23,
            ]}
            labels={usersPerHour.map((_, i) => i.toString())}
            sessions={[]}
            events={[]}
            granularity="hour"
          />
          <div className="grid grid-cols-1 md:grid-cols-2 gap-[1px] pt-[1px] bg-accent">
            <div className={containerClassName}>
              <TopNChart
                items={[
                  { name: "US", value: 731 },
                  { name: "CA", value: 583 },
                  { name: "GB", value: 255 },
                  { name: "ES", value: 195 },
                  { name: "BR", value: 79 },
                  { name: "AU", value: 64 },
                  { name: "DE", value: 50 },
                ]}
                title="Countries"
                valueLabel="Sessions"
                renderRow={(item) => <Country countryCode={item.name} />}
              />
            </div>
            <div className={containerClassName}>
              <TopNChart
                items={[
                  { name: "macOS", value: 646 },
                  { name: "Windows", value: 545 },
                  { name: "iOS", value: 343 },
                  { name: "Android", value: 251 },
                  { name: "Ubuntu", value: 176 },
                  { name: "iPadOS", value: 42 },
                ]}
                title="Operating Systems"
                valueLabel="Sessions"
                renderRow={(item) => <OS name={item.name} />}
              />
            </div>
            <div className={containerClassName}>
              <TopNChart
                items={[
                  { name: "app_started", value: 5215 },
                  { name: "screen_view", value: 2432 },
                  { name: "reminder_created", value: 984 },
                  { name: "paywall_shown", value: 680 },
                  { name: "subscribed", value: 453 },
                ]}
                title="Events"
                valueLabel="Count"
              />
            </div>
            <div className={containerClassName}>
              <TopNChart
                items={[
                  { name: "1.4.1", value: 533 },
                  { name: "1.4.0", value: 423 },
                  { name: "1.3.5", value: 156 },
                  { name: "1.3.2", value: 46 },
                  { name: "1.0.0", value: 12 },
                ]}
                title="App Versions"
                valueLabel="Count"
              />
            </div>
          </div>
        </div>
      </div>
    </Page>
  );
}
