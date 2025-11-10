import { useEffect, useState } from "react";
import { SignInWithAuthentik } from "./SignInWithAuthentik";
import { SignInWithGitHub } from "./SignInWithGitHub";
import { SignInWithGoogle } from "./SignInWithGoogle";
import { DEFAULT_OAUTH_STATUS } from "./auth";

type OAuthStatus = {
  github: boolean;
  google: boolean;
  authentik: boolean;
  emailAuthDisabled: boolean;
};

export function OAuthButtons() {
  const [oauthStatus, setOauthStatus] = useState<OAuthStatus | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const checkOAuthStatus = async () => {
      try {
        const response = await fetch("/api/_auth/oauth-status");
        if (response.ok) {
          const status = await response.json();
          setOauthStatus(status);
        } else {
          setOauthStatus(DEFAULT_OAUTH_STATUS);
        }
      } catch {
        setOauthStatus(DEFAULT_OAUTH_STATUS);
      } finally {
        setLoading(false);
      }
    };

    checkOAuthStatus();
  }, []);

  // Show loading state or no buttons if still loading
  if (loading) {
    return null;
  }

  // Don't show anything if no OAuth providers are available
  if (!oauthStatus || (!oauthStatus.github && !oauthStatus.google && !oauthStatus.authentik)) {
    return null;
  }

  return (
    <>
      <div className="space-y-2">
        {oauthStatus.github && <SignInWithGitHub />}
        {oauthStatus.google && <SignInWithGoogle />}
        {oauthStatus.authentik && <SignInWithAuthentik />}
      </div>

      {!oauthStatus.emailAuthDisabled && (
        <div className="relative my-4">
          <div className="absolute inset-0 flex items-center" aria-hidden="true">
            <div className="w-full border-t" />
          </div>
          <div className="relative flex justify-center text-sm">
            <span className="px-2 bg-muted">OR</span>
          </div>
        </div>
      )}
    </>
  );
}
