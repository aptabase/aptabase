import { api } from "@fns/api";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { OwnershipTransferRequestsModal } from "./OwnershipTransferRequestsModal";

type IncomingTransferRequest = {
  appId: string;
  appName: string;
  currentOwnerEmail: string;
  requestedAt: string;
};

export function OwnershipTransferStartupCheck() {
  const [showModal, setShowModal] = useState(false);

  const { data: requests } = useQuery({
    queryKey: ["ownershipTransferRequestsStartup"],
    queryFn: () => api.get<IncomingTransferRequest[]>("/_ownership-transfer-requests"),
    refetchOnWindowFocus: false,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  useEffect(() => {
    // automatically show modal if there are pending requests
    if (requests && requests.length > 0) {
      setShowModal(true);
    }
  }, [requests]);

  return <OwnershipTransferRequestsModal open={showModal} onClose={() => setShowModal(false)} />;
}
