import { useState } from "react";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { Button } from "@components/Button";
import { LoadingState } from "@components/LoadingState";
import { EmptyState } from "@components/EmptyState";
import { ErrorState } from "@components/ErrorState";
import {
  IconChevronLeft,
  IconChevronRight,
  IconAlertTriangle,
  IconClock,
  IconDeviceMobile,
} from "@tabler/icons-react";

interface ErrorItem {
  errorId: string;
  appId: string;
  timestamp: string;
  errorMessage: string;
  errorType: string;
  stackTrace: string;
  platform: string;
  osName: string;
  osVersion: string;
  appVersion: string;
  sdkVersion: string;
  sessionId: string;
}

interface ErrorsResponse {
  errors: ErrorItem[];
  pagination: {
    offset: number;
    limit: number;
    total: number;
  };
}

async function fetchErrors(
  appId: string,
  offset: number,
  limit: number
): Promise<ErrorsResponse> {
  const params = new URLSearchParams({
    offset: offset.toString(),
    limit: limit.toString(),
  });

  const response = await fetch(`/api/v0/apps/${appId}/errors?${params}`, {
    credentials: "include",
  });

  if (!response.ok) {
    throw new Error("Failed to fetch errors");
  }

  return response.json();
}

interface ErrorsListProps {
  appId: string;
}

export function ErrorsList({ appId }: ErrorsListProps) {
  const [offset, setOffset] = useState(0);
  const limit = 50;

  const { data, isLoading, isError, refetch, isPlaceholderData } = useQuery({
    queryKey: ["errors", appId, offset, limit],
    queryFn: () => fetchErrors(appId, offset, limit),
    placeholderData: keepPreviousData,
  });

  const handlePreviousPage = () => {
    if (offset - limit >= 0) {
      setOffset(offset - limit);
    }
  };

  const handleNextPage = () => {
    if (data && offset + limit < data.pagination.total) {
      setOffset(offset + limit);
    }
  };

  const hasNextPage = data && offset + limit < data.pagination.total;
  const hasPreviousPage = offset > 0;

  if (isLoading) {
    return <LoadingState size="lg" />;
  }

  if (isError) {
    return <ErrorState refetch={refetch} />;
  }

  if (!data || data.errors.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="mt-6">
      <div className="flow-root">
        <div className="overflow-x-auto">
          <div className="inline-block min-w-full py-2 align-middle">
            <table className="min-w-full divide-y divide-gray-300">
              <thead>
                <tr>
                  <th className="py-3.5 pl-4 pr-3 text-left text-sm font-semibold">
                    <div className="flex items-center gap-2">
                      <IconClock className="text-muted-foreground h-5 w-5" />
                      Timestamp
                    </div>
                  </th>
                  <th className="px-3 py-3.5 text-left text-sm font-semibold">
                    <div className="flex items-center gap-2">
                      <IconAlertTriangle className="text-muted-foreground h-5 w-5" />
                      Error Type
                    </div>
                  </th>
                  <th className="px-3 py-3.5 text-left text-sm font-semibold">
                    Message
                  </th>
                  <th className="px-3 py-3.5 text-left text-sm font-semibold">
                    <div className="flex items-center gap-2">
                      <IconDeviceMobile className="text-muted-foreground h-5 w-5" />
                      Platform
                    </div>
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-600">
                {data.errors.map((error) => (
                  <tr
                    key={error.errorId}
                    className="hover:bg-accent cursor-pointer"
                  >
                    <td className="whitespace-nowrap py-4 pl-4 pr-3 text-sm">
                      {new Date(error.timestamp).toLocaleString()}
                    </td>
                    <td className="whitespace-nowrap px-3 py-4 text-sm">
                      {error.errorType}
                    </td>
                    <td className="px-3 py-4 text-sm max-w-md truncate">
                      {error.errorMessage}
                    </td>
                    <td className="whitespace-nowrap px-3 py-4 text-sm">
                      {error.platform}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <div className="flex justify-between items-center mt-4">
        <div className="text-sm text-muted-foreground">
          Showing {offset + 1} to {Math.min(offset + limit, data.pagination.total)} of{" "}
          {data.pagination.total} errors
        </div>
        <div className="flex gap-2">
          <Button
            disabled={!hasPreviousPage || isPlaceholderData}
            variant="ghost"
            onClick={handlePreviousPage}
          >
            <IconChevronLeft className="h-4 w-4" />
            Previous
          </Button>
          <Button
            disabled={!hasNextPage || isPlaceholderData}
            variant="ghost"
            onClick={handleNextPage}
          >
            Next
            <IconChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}
