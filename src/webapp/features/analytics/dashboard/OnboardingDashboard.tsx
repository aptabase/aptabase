import { Application, getAppById } from "@features/apps";
import { useQuery } from "@tanstack/react-query";
import { WaitingForEventsContent } from "./WaitingForEventsContent";
import { TeaserDashboardContainer } from "./TeaserDashboardContainer";

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

  return (
    <TeaserDashboardContainer app={props.app}>
      <WaitingForEventsContent appId={props.app.id} />
    </TeaserDashboardContainer>
  );
}
