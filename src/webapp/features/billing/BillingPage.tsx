import { Page, PageHeading } from "@components/Page";
import { CurrentUsage } from "./CurrentUsage";
import { CurrentPlan } from "./CurrentPlan";
import { BillingHistoricalUsage, BillingInfo, useBilling, useHistoricalData } from "./useBilling";
import { ErrorState } from "@components/ErrorState";
import { LoadingState } from "@components/LoadingState";
import { MonthlyUsageChart } from "./MonthlyUsageChart";
import { formatPeriod } from "@fns/format-date";

Component.displayName = "BillingPage";
export function Component() {
  const { isLoading: isLoadingBilling, isError: isErrorBilling, data: billing } = useBilling();
  const { isLoading: isLoadingHistorical, isError: isErrorHistorical, data: historical } = useHistoricalData();

  const isLoading = isLoadingBilling || isLoadingHistorical;
  const isError = isErrorBilling || isErrorHistorical;

  return (
    <Page title="Billing">
      <PageHeading title="Billing" subtitle="Manage your subscription" />

      <div className="flex flex-col gap-8 max-w-3xl mt-8">
        {isLoading && <LoadingState />}
        {isError && <ErrorState />}
        {billing && historical && <Body billing={billing} historical={historical} />}
      </div>
    </Page>
  );
}

function Body(props: { billing: BillingInfo; historical: BillingHistoricalUsage[] }) {
  return (
    <>
      <div className="grid lg:grid-cols-2 items-stretch gap-4">
        <div className="border rounded-md p-4 min-h-[6rem]">
          <CurrentPlan billing={props.billing} />
        </div>
        <div className="border rounded-md p-4 min-h-[6rem]">
          <CurrentUsage
            month={props.billing.month}
            year={props.billing.year}
            state={props.billing.state}
            usage={props.billing.usage}
            quota={props.billing.plan.monthlyEvents}
          />
        </div>
      </div>
      <div>
        <p>Monthly Usage</p>
        <div className="h-60 mt-4">
          <MonthlyUsageChart
            dates={props.historical.map((x) => formatPeriod("month", x.date))}
            events={props.historical.map((x) => x.events)}
            state={props.billing.state}
            quota={props.billing.plan.monthlyEvents}
          />
        </div>
      </div>
    </>
  );
}
