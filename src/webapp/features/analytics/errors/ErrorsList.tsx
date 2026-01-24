import { useState } from "react";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { useAtomValue } from "jotai";
import { dateFilterValuesAtom } from "../../../atoms/date-atoms";
import { Button } from "@components/Button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@components/Select";
import { LoadingState } from "@components/LoadingState";
import { EmptyState } from "@components/EmptyState";
import { ErrorState } from "@components/ErrorState";
import {
  IconChevronLeft,
  IconChevronRight,
  IconAlertTriangle,
  IconClock,
  IconDeviceMobile,
  IconFilter,
} from "@tabler/icons-react";
import { DateFilterContainer } from "../date-filters/DateFilterContainer";

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
  platform?: string,
  errorType?: string
): Promise<ErrorsResponse> {
  const params = new URLSearchParams({
    offset: offset.toString(),
    limit: limit.toString(),
  });

  if (startDate) params.set("startDate", startDate);
  if (endDate) params.set("endDate", endDate);
  if (platform && platform !== "all") params.set("platform", platform);
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

  // Get filter values from URL params
  const platform = searchParams.get("platform") || "all";
  const errorType = searchParams.get("errorType") || "all";

  const { data, isLoading, isError, refetch, isPlaceholderData } = useQuery({
    queryKey: [
      "errors",
      appId,
      offset,
      limit,
      dateFilters.startDateIso,
      dateFilters.endDateIso,
      platform,
      errorType,
    ],
    queryFn: () =>
      fetchErrors(
        appId,
        offset,
        limit,
        dateFilters.startDateIso,
        dateFilters.endDateIso,
        platform !== "all" ? platform : undefined,
        errorType !== "all" ? errorType : undefined
      ),
    placeholderData: keepPreviousData,
  });

  const handlePlatformChange = (value: string) => {
    const newParams = new URLSearchParams(searchParams);
    if (value === "all") {
      newParams.delete("platform");
    } else {
      newParams.set("platform", value);
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

  if (!data || data.errors.length === 0) {
    return <EmptyState />;
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
          <span className="text-sm text-muted-foreground">Platform:</span>
          <Select value={platform} onValueChange={handlePlatformChange}>
            <SelectTrigger className="w-40">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Platforms</SelectItem>
              <SelectItem value="iOS">iOS</SelectItem>
              <SelectItem value="Android">Android</SelectItem>
              <SelectItem value="Windows">Windows</SelectItem>
              <SelectItem value="macOS">macOS</SelectItem>
              <SelectItem value="Linux">Linux</SelectItem>
              <SelectItem value="Web">Web</SelectItem>
            </SelectContent>
          </Select>
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
