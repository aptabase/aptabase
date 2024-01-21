## Setting up a development environment

1. Install [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
2. Install [Node.js](https://nodejs.org/en/download/)
3. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/)
4. Clone the repo
5. Start all dependencies with `docker compose up -d`
6. Navigate to `src/` and run `npm install` and `dotnet restore` to install dependencies
7. While on `src/`, run `dotnet watch` to start the server in watch mode
8. In another terminal, also on `src/`, run `npm run dev` to start the client in watch mode
9. Navigate to `http://localhost:3000` to view the app
10. That's it! You're ready to start writing some code :)

**Note:** Emails are sent via Mailcatcher, the UI is available at `http://localhost:1080`
