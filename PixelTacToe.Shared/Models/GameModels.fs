namespace PixelTacToe.Shared.Models

open System

[<CLIMutable>]
type Player = {
    Id: string
    Name: string
    ImageUrl: string
    IsSearching: bool
}

[<CLIMutable>]
type GameState = {
    Id: string
    Board: string[,]
    CurrentTurn: string
    Player1: string
    Player2: string
    WinnerPlayerId: Option<string>
}

type PlayerMarker = X | O 