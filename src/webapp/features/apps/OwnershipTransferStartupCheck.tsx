import { api } from "@fns/api";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { AppRequestPurpose, IncomingAppRequest } from "./app-requests";
import { useApps } from "./AppsProvider";
import { OwnershipTransferRequestsModal } from "./OwnershipTransferRequestsModal";

export function OwnershipTransferStartupCheck() {
  const [showModal, setShowModal] = useState(false);
  const { refetchApps } = useApps();

  const { data: requests, isSuccess } = useQuery({
    queryKey: ["appRequests", AppRequestPurpose.AppOwnership],
    queryFn: () => api.get<IncomingAppRequest[]>(`/_app-requests?purpose=${AppRequestPurpose.AppOwnership}`),
    refetchOnWindowFocus: true,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  // show modal if there are pending requests and user hasn't seen them yet
  const hasRequests = isSuccess && requests && requests.length > 0;
  if (hasRequests && !showModal) {
    setShowModal(true);
  }

  const closeHandler = () => {
    setShowModal(false);
    refetchApps();
  };

  return <OwnershipTransferRequestsModal open={showModal} onClose={closeHandler} />;
}
