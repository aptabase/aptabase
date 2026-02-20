import { Button } from "@components/Button";
import { EmptyState } from "@components/EmptyState";
import { ErrorState } from "@components/ErrorState";
import { LoadingState } from "@components/LoadingState";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@components/Select";
import {
  IconAlertTriangle,
  IconChevronLeft,
  IconChevronRight,
  IconClock,
  IconDeviceDesktop,
  IconFilter,
} from "@tabler/icons-react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useAtomValue } from "jotai";
import { useState } from "react";
import { useSearchParams } from "react-router-dom";
import { dateFilterValuesAtom } from "../../../atoms/date-atoms";
import { OSIcon } from "../dashboard/icons/os";
import { DateFilterContainer } from "../date-filters/DateFilterContainer";
import { OsFilterDropdown } from "../sessions/filters/OsFilterDropdown";
import { ErrorDetailModal } from "./ErrorDetailModal";

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
  limit: number,
  startDate?: string,
  endDate?: string,
  osName?: string,
  errorType?: string,
): Promise<ErrorsResponse> {
  const params = new URLSearchParams({
    offset: offset.toString(),
    limit: limit.toString(),
  });

  if (startDate) params.set("startDate", startDate);
  if (endDate) params.set("endDate", endDate);
  if (osName && osName !== "all") params.set("osName", osName);
  if (errorType && errorType !== "all") params.set("errorType", errorType);

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
  const [searchParams, setSearchParams] = useSearchParams();
  const dateFilters = useAtomValue(dateFilterValuesAtom);
  const [selectedErrorId, setSelectedErrorId] = useState<string | null>(null);

  // Get filter values from URL params
  const osName = searchParams.get("osName") || "all";
  const errorType = searchParams.get("errorType") || "all";

  const { data, isLoading, isError, refetch, isPlaceholderData } = useQuery({
    queryKey: ["errors", appId, offset, limit, dateFilters.startDateIso, dateFilters.endDateIso, osName, errorType],
    queryFn: () =>
      fetchErrors(
        appId,
        offset,
        limit,
        dateFilters.startDateIso,
        dateFilters.endDateIso,
        osName !== "all" ? osName : undefined,
        errorType !== "all" ? errorType : undefined,
      ),
    placeholderData: keepPreviousData,
  });

  const handleOsNameChange = (value: string) => {
    const newParams = new URLSearchParams(searchParams);
    if (value === "all" || !value) {
      newParams.delete("osName");
    } else {
      newParams.set("osName", value);
    }
    setSearchParams(newParams);
    setOffset(0); // Reset to first page when filters change
  };

  const handleErrorTypeChange = (value: string) => {
    const newParams = new URLSearchParams(searchParams);
    if (value === "all") {
      newParams.delete("errorType");
    } else {
      newParams.set("errorType", value);
    }
    setSearchParams(newParams);
    setOffset(0); // Reset to first page when filters change
  };

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

  return (
    <div className="mt-6">
      {/* Filters */}
      <div className="mb-6 flex flex-wrap items-end gap-4">
        <div className="flex items-center gap-2">
          <IconFilter className="h-5 w-5 text-muted-foreground" />
          <span className="text-sm font-medium">Filters:</span>
        </div>

        <div className="flex items-center gap-2">
          <span className="text-sm text-muted-foreground">Date Range:</span>
          <DateFilterContainer />
        </div>

        <div className="flex items-center gap-2">
          <span className="text-sm text-muted-foreground">OS:</span>
          <OsFilterDropdown appId={appId} onValueChange={(osName) => handleOsNameChange(osName ?? "all")} />
        </div>

        <div className="flex items-center gap-2">
          <span className="text-sm text-muted-foreground">Error Type:</span>
          <Select value={errorType} onValueChange={handleErrorTypeChange}>
            <SelectTrigger className="w-40">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Types</SelectItem>
              {/* Note: In a real implementation, you might want to fetch unique error types from the API */}
              <SelectItem value="RuntimeError">RuntimeError</SelectItem>
              <SelectItem value="TypeError">TypeError</SelectItem>
              <SelectItem value="NetworkError">NetworkError</SelectItem>
              <SelectItem value="SyntaxError">SyntaxError</SelectItem>
              <SelectItem value="ReferenceError">ReferenceError</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {(!data || data.errors.length === 0) && <EmptyState />}

      {data && data.errors.length > 0 && (
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
                    <th className="px-3 py-3.5 text-left text-sm font-semibold">Message</th>
                    <th className="px-3 py-3.5 text-left text-sm font-semibold">
                      <div className="flex items-center gap-2">
                        <IconDeviceDesktop className="text-muted-foreground h-5 w-5" />
                        OS
                      </div>
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-600">
                  {data.errors.map((error) => (
                    <tr
                      key={error.errorId}
                      className="hover:bg-accent cursor-pointer"
                      onClick={() => setSelectedErrorId(error.errorId)}
                    >
                      <td className="whitespace-nowrap py-4 pl-4 pr-3 text-sm">
                        {new Date(error.timestamp).toLocaleString()}
                      </td>
                      <td className="whitespace-nowrap px-3 py-4 text-sm">{error.errorType}</td>
                      <td className="px-3 py-4 text-sm max-w-md truncate">{error.errorMessage}</td>
                      <td className="whitespace-nowrap px-3 py-4 text-sm">
                        <div className="flex items-center gap-2">
                          <span>
                            {error.osName} {error.osVersion}
                          </span>
                          <OSIcon name={error.osName} className="h-5 w-5" />
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      <div className="flex justify-between items-center mt-4">
        <div className="text-sm text-muted-foreground">
          Showing {offset + 1} to {Math.min(offset + limit, data.pagination.total)} of {data.pagination.total} errors
        </div>
        <div className="flex gap-2">
          <Button disabled={!hasPreviousPage || isPlaceholderData} variant="ghost" onClick={handlePreviousPage}>
            <IconChevronLeft className="h-4 w-4" />
            Previous
          </Button>
          <Button disabled={!hasNextPage || isPlaceholderData} variant="ghost" onClick={handleNextPage}>
            Next
            <IconChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>

      <ErrorDetailModal
        appId={appId}
        errorId={selectedErrorId}
        open={!!selectedErrorId}
        onClose={() => setSelectedErrorId(null)}
      />
    </div>
  );
}
