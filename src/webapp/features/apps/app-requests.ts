export enum AppRequestPurpose {
  AppOwnership = "AppOwnership",
  AppShare = "AppShare",
}

export type AppRequest = {
  targetUserEmail: string;
  requestedAt: string;
  status: string;
  purpose: AppRequestPurpose;
};

export type IncomingAppRequest = {
  appId: string;
  appName: string;
  requesterEmail: string;
  requestedAt: string;
  purpose: AppRequestPurpose;
};
