interface CrispClient {
  push: (args: Array<string, string[]>) => void;
}

interface Window {
  $crisp?: CrispClient;
}
