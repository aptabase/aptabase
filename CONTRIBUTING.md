## Setting up a development environment

1. Install [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
2. Install [Node.js](https://nodejs.org/en/download/)
3. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/)
4. Clone the repo
5. Start all dependencies with `docker compose up -d`
6. Copy `src/Properties/launchSettings.example.json` to `src/Properties/launchSettings.json`
7. Navigate to `src/` and run `npm install` and `dotnet restore` to install dependencies
8. While on `src/`, run `dotnet watch` to start the server in watch mode
9. In another terminal, also on `src/`, run `npm run dev` to start the client in watch mode
10. Navigate to `http://localhost:3000` to view the app

That's it! You're ready to start writing some code 🚀

**Note:** Emails are sent via Mailcatcher, the UI is available at `http://localhost:1080`

## Getting some work done

We have a very light process for contributing to Aptabase, simply find an issue you want to work on, ask for it to be assigned to you, and submit a PR when you're done. We'll review it and merge it as soon as possible. If you can't find an issue you want to work on, feel free to open one and we'll discuss it with you.

We recommend you to give a brief description of how you plan to solve the issue before starting to work on it. This will help us to avoid wasting time on PRs that don't get merged.

🚀
