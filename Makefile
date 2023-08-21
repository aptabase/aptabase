dev: vite-dev dotnet-dev

vite-dev:
	cd ./src && npm run dev

dotnet-dev:
	cd ./src && DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1 dotnet watch