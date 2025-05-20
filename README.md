# PixelTacToe-Online

A simple, browser-based Tic-Tac-Toe game built with an F# ASP.NET Core backend and vanilla JavaScript frontend. Players can upload a picture, which will later become their marker, see who’s online in the lobby, send and accept match requests, and play games tracked on the server.

## Live Demo

Browse to [http://109.122.217.232:5000/](http://109.122.217.232:5000/) to try out.

## Features

- **User Registration**: Upload a profile image and choose a display name.
- **Lobby**: See online players (heartbeat-based presence) and send game requests.
- **Matchmaking**: Receive match requests.
- **Game Sessions**: Play Tic-Tac-Toe each game state is stored as JSON on the server.
- **Leaderboard**: Global top-20 players ranked by wins, losses, and draws.
- **Automatic Cleanup**: Temporary upload and game folders are cleared on server startup.

## Architecture

- **Backend**: F# on ASP.NET Core 8, with controllers for players and games. Uses filesystem storage (`wwwroot/uploads/*.json` and `wwwroot/games/*.json`) for simplicity.
- **Frontend**: Vanilla JavaScript, HTML, and CSS (no frameworks). Communicates via RESTful APIs and polls periodically for updates.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Getting Started

> ⚙️ **Tip:** Start the server from the `Server/` directory and the client from the `Client/` directory.

**Clone the repo**

```bash
Serve the server:
cd repo/Server
dotnet run
The API will listen on http://localhost:5246 by default.


Serve the client:

cd ../Client
dotnet run
Then browse to http://localhost:5000