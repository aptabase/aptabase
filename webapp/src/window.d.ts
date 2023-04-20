interface CrispClient {
  push: (args: string[]) => void;
}

interface Window {
  $crisp?: CrispClient;
}
