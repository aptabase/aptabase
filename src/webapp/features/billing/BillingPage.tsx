import { Button } from "@components/Button";
import { ErrorState } from "@components/ErrorState";
import { LoadingState } from "@components/LoadingState";
import { Page, PageHeading } from "@components/Page";
import { formatPeriod } from "@fns/format-date";
import { useState } from "react";
import { CurrentPlan } from "./CurrentPlan";
import { CurrentUsage } from "./CurrentUsage";
import { DeleteAccountModal } from "./DeleteAccountModal";
import { MonthlyUsageChart } from "./MonthlyUsageChart";
import { BillingHistoricalUsage, BillingInfo, useBilling, useHistoricalData } from "./useBilling";

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
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const openDeleteModal = () => setShowDeleteModal(true);
  const closeDeleteModal = () => setShowDeleteModal(false);

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

        <div className="flex items-center justify-end text-xs text-muted-foreground gap-4 mt-2 mr-4">
          <div className="space-x-1">
            <div className="bg-success w-2 h-2 rounded-full inline-block" />
            <span>Limit</span>
          </div>

          <div className="space-x-1">
            <div className="bg-primary w-2 h-2 rounded-full inline-block" />
            <span>Usage</span>
          </div>
        </div>
      </div>

      <div className="mt-12">
        <PageHeading title="Danger Zone" subtitle="Be careful with these actions" />
        <div className="border-[0.5px] border-destructive rounded-md mt-4">
          <DangerZoneItem
            title="Delete Account"
            description="Once you delete your account, there is no going back."
            subDescription="All events and associated data will be permanently erased from our database."
            actionText="Delete account"
            onClick={openDeleteModal}
          />
        </div>
      </div>

      <DeleteAccountModal open={showDeleteModal} onClose={closeDeleteModal} />
    </>
  );
}

function DangerZoneItem({
  title,
  description,
  subDescription,
  actionText,
  onClick,
}: {
  title: string;
  description: string;
  subDescription: string;
  actionText: string;
  onClick: () => void;
}) {
  return (
    <div className="flex items-center justify-between p-4 border-b border-destructive last:border-b-0">
      <div>
        <h3 className="font-semibold">{title}</h3>
        <p className="text-sm text-muted-foreground">{description}</p>
        <p className="text-sm text-muted-foreground">{subDescription}</p>
      </div>
      <Button variant="destructive" onClick={onClick}>
        {actionText}
      </Button>
    </div>
  );
}
