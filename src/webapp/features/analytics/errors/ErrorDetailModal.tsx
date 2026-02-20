import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@components/Dialog";
import { LoadingState } from "@components/LoadingState";
import { IconCheck, IconCopy } from "@tabler/icons-react";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { useNavigate } from "react-router-dom";

interface ErrorDetail {
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

interface ErrorDetailModalProps {
  appId: string;
  errorId: string | null;
  open: boolean;
  onClose: () => void;
}

async function fetchErrorDetail(appId: string, errorId: string): Promise<ErrorDetail> {
  const response = await fetch(`/api/v0/apps/${appId}/errors/${errorId}`, {
    credentials: "include",
  });

  if (!response.ok) {
    throw new Error("Failed to fetch error details");
  }

  return response.json();
}

export function ErrorDetailModal({ appId, errorId, open, onClose }: ErrorDetailModalProps) {
  const [justCopied, setJustCopied] = useState(false);
  const navigate = useNavigate();

  const { data: error, isLoading } = useQuery({
    queryKey: ["error-detail", appId, errorId],
    queryFn: () => fetchErrorDetail(appId, errorId!),
    enabled: !!errorId && open,
  });

  const handleCopyStackTrace = () => {
    if (error?.stackTrace) {
      navigator.clipboard.writeText(error.stackTrace);
      setJustCopied(true);
      setTimeout(() => setJustCopied(false), 2000);
    }
  };

  const handleClickSessionId = (sessionId: string) => () => {
    // Get the current location to preserve search params
    const currentLocation = window.location;

    navigate(`/${appId}/live/${sessionId}`, {
      state: {
        returnTo: {
          pathname: currentLocation.pathname,
          search: currentLocation.search,
        },
      },
    });
  };

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
        {isLoading ? (
          <div className="py-8">
            <LoadingState size="md" />
          </div>
        ) : error ? (
          <>
            <DialogHeader>
              <DialogTitle>Error Details</DialogTitle>
              <DialogDescription>{error.errorType}</DialogDescription>
            </DialogHeader>

            <div className="space-y-6 mt-4">
              {/* Error Information Section */}
              <div>
                <h3 className="text-sm font-semibold mb-3">Error Information</h3>
                <div className="space-y-2 text-sm">
                  <div className="grid grid-cols-4 gap-2">
                    <span className="text-muted-foreground">Timestamp:</span>
                    <span className="col-span-3">{new Date(error.timestamp).toLocaleString()}</span>
                  </div>
                  <div className="grid grid-cols-4 gap-2">
                    <span className="text-muted-foreground">Error Type:</span>
                    <span className="col-span-3 font-medium">{error.errorType}</span>
                  </div>
                  <div className="grid grid-cols-4 gap-2">
                    <span className="text-muted-foreground">Message:</span>
                    <span className="col-span-3">{error.errorMessage}</span>
                  </div>
                </div>
              </div>

              {/* Device/Platform Section */}
              <div>
                <h3 className="text-sm font-semibold mb-3">Device & Platform Information</h3>
                <div className="space-y-2 text-sm">
                  <div className="grid grid-cols-4 gap-2">
                    <span className="text-muted-foreground">Platform:</span>
                    <span className="col-span-3">{error.platform}</span>
                  </div>
                  <div className="grid grid-cols-4 gap-2">
                    <span className="text-muted-foreground">OS:</span>
                    <span className="col-span-3">
                      {error.osName} {error.osVersion}
                    </span>
                  </div>
                  <div className="grid grid-cols-4 gap-2">
                    <span className="text-muted-foreground">App Version:</span>
                    <span className="col-span-3">{error.appVersion}</span>
                  </div>
                  <div className="grid grid-cols-4 gap-2">
                    <span className="text-muted-foreground">SDK Version:</span>
                    <span className="col-span-3">{error.sdkVersion}</span>
                  </div>
                  {error.sessionId && (
                    <div className="grid grid-cols-4 gap-2">
                      <span className="text-muted-foreground">Session ID:</span>
                      <button
                        className="col-span-3 font-mono text-xs text-left"
                        onClick={handleClickSessionId(error.sessionId)}
                      >
                        {error.sessionId}
                      </button>
                    </div>
                  )}
                </div>
              </div>

              {/* Stack Trace Section */}
              {error.stackTrace && (
                <div>
                  <div className="flex items-center justify-between mb-3">
                    <h3 className="text-sm font-semibold">Stack Trace</h3>
                    <button
                      onClick={handleCopyStackTrace}
                      className="flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors"
                    >
                      {justCopied ? (
                        <>
                          <IconCheck className="h-4 w-4" />
                          <span>Copied!</span>
                        </>
                      ) : (
                        <>
                          <IconCopy className="h-4 w-4" />
                          <span>Copy</span>
                        </>
                      )}
                    </button>
                  </div>
                  <pre className="bg-muted p-4 rounded-md text-xs font-mono overflow-x-auto whitespace-pre-wrap break-words">
                    {error.stackTrace}
                  </pre>
                </div>
              )}
            </div>
          </>
        ) : (
          <div className="py-8 text-center text-muted-foreground">Error not found</div>
        )}
      </DialogContent>
    </Dialog>
  );
}
