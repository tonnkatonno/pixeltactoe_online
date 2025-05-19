namespace PixelTacToe.Server.Services

open System
open System.Threading.Tasks
open System.Text.Json
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.Logging
open PixelTacToe.Server.Models
open PixelTacToe.Shared.Models
open PixelTacToe.Shared.Constants

type GameRepository(context: PixelTacToeDbContext, logger: ILogger<GameRepository>) =
    
    member this.CreateGameAsync(game: GameState) =
        task {
            try
                let now = DateTime.UtcNow
                
                let gameEntity = {
                    Id = game.Id
                    Player1Id = game.Player1
                    Player2Id = game.Player2
                    WinnerPlayerId = None
                    StartedAt = now
                    EndedAt = None
                    FinalBoardState = ""
                }
                
                do! context.Games.AddAsync(gameEntity) |> Async.AwaitTask
                do! context.SaveChangesAsync() |> Async.AwaitTask |> Async.Ignore
                
                return Ok gameEntity
            with
            | ex ->
                logger.LogError(ex, "Error creating game")
                return Error "Failed to create game"
        }
        
    member this.RecordMoveAsync(gameId: string, playerId: string, row: int, col: int) =
        task {
            try
                let! moves = 
                    context.GameMoves
                        .Where(fun m -> m.GameId = gameId)
                        .OrderByDescending(fun m -> m.MoveNumber)
                        .Take(1)
                        .ToListAsync()
                        
                let moveNumber = 
                    if moves.Count > 0 then
                        moves.[0].MoveNumber + 1
                    else
                        1
                
                let moveEntity = {
                    Id = 0
                    GameId = gameId
                    PlayerId = playerId
                    Row = row
                    Col = col
                    MoveNumber = moveNumber
                    Timestamp = DateTime.UtcNow
                }
                
                do! context.GameMoves.AddAsync(moveEntity) |> Async.AwaitTask
                do! context.SaveChangesAsync() |> Async.AwaitTask |> Async.Ignore
                
                return Ok moveEntity
            with
            | ex ->
                logger.LogError(ex, "Error recording move")
                return Error "Failed to record move"
        }
        
    member this.EndGameAsync(gameId: string, winnerPlayerId: string option, finalBoard: string[,]) =
        task {
            try
                let! game = context.Games.FindAsync(gameId) |> Async.AwaitTask
                
                if isNull game then
                    return Error "Game not found"
                else
                    let boardJson = JsonSerializer.Serialize(finalBoard)
                    
                    game.WinnerPlayerId <- winnerPlayerId
                    game.EndedAt <- Some (DateTime.UtcNow)
                    game.FinalBoardState <- boardJson
                    
                    do! context.SaveChangesAsync() |> Async.AwaitTask |> Async.Ignore
                    
                    return Ok game
            with
            | ex ->
                logger.LogError(ex, "Error ending game")
                return Error "Failed to end game"
        }
        
    member this.GetPlayerGameHistoryAsync(playerId: string, pageSize: int, pageNumber: int) =
        task {
            try
                let skip = (pageNumber - 1) * pageSize
                
                let! games = 
                    context.Games
                        .Where(fun g -> g.Player1Id = playerId || g.Player2Id = playerId)
                        .OrderByDescending(fun g -> g.StartedAt)
                        .Skip(skip)
                        .Take(pageSize)
                        .ToListAsync()
                        
                return Ok games
            with
            | ex ->
                logger.LogError(ex, "Error retrieving player game history")
                return Error "Failed to retrieve game history"
        }
        
    member this.GetGameReplayAsync(gameId: string) =
        task {
            try
                let! moves = 
                    context.GameMoves
                        .Where(fun m -> m.GameId = gameId)
                        .OrderBy(fun m -> m.MoveNumber)
                        .ToListAsync()
                        
                return Ok moves
            with
            | ex ->
                logger.LogError(ex, "Error retrieving game replay")
                return Error "Failed to retrieve game replay"
        } 