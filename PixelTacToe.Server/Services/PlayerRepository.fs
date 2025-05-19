namespace PixelTacToe.Server.Services

open System
open System.Threading.Tasks
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.Logging
open PixelTacToe.Server.Models
open PixelTacToe.Shared.Models

type PlayerRepository(context: PixelTacToeDbContext, logger: ILogger<PlayerRepository>) =
    
    member this.CreateOrUpdatePlayerAsync(player: Player) =
        task {
            try
                let now = DateTime.UtcNow
                let! existingPlayer = context.Players.FindAsync(player.Id)
                
                if isNull existingPlayer then
                    let playerEntity = {
                        Id = player.Id
                        Name = player.Name
                        ImageUrl = player.ImageUrl
                        CreatedAt = now
                        LastActiveAt = now
                        GamesPlayed = 0
                        GamesWon = 0
                        GamesDraw = 0
                    }
                    
                    do! context.Players.AddAsync(playerEntity)
                    do! context.SaveChangesAsync() |> Async.AwaitTask |> Async.Ignore
                    
                    return Ok playerEntity
                else
                    existingPlayer.Name <- player.Name
                    existingPlayer.ImageUrl <- player.ImageUrl
                    existingPlayer.LastActiveAt <- now
                    
                    do! context.SaveChangesAsync() |> Async.AwaitTask |> Async.Ignore
                    
                    return Ok existingPlayer
            with
            | ex ->
                logger.LogError(ex, "Error creating or updating player")
                return Error "Failed to save player data"
        }
        
    member this.GetPlayerByIdAsync(playerId: string) =
        task {
            try
                let! player = context.Players.FindAsync(playerId)
                return 
                    if isNull player then None
                    else Some player
            with
            | ex ->
                logger.LogError(ex, "Error retrieving player by ID")
                return None
        }
        
    member this.UpdatePlayerStatsAsync(playerId: string, isWinner: bool, isDraw: bool) =
        task {
            try
                let! player = context.Players.FindAsync(playerId)
                
                if not (isNull player) then
                    player.GamesPlayed <- player.GamesPlayed + 1
                    
                    if isWinner then
                        player.GamesWon <- player.GamesWon + 1
                    elif isDraw then
                        player.GamesDraw <- player.GamesDraw + 1
                        
                    player.LastActiveAt <- DateTime.UtcNow
                    
                    do! context.SaveChangesAsync() |> Async.AwaitTask |> Async.Ignore
                    return Ok()
                else
                    return Error "Player not found"
            with
            | ex ->
                logger.LogError(ex, "Error updating player stats")
                return Error "Failed to update player stats"
        }
        
    member this.ToPlayerModel(entity: PlayerEntity) =
        {
            Id = entity.Id
            Name = entity.Name
            ImageUrl = entity.ImageUrl
            IsSearching = false
        } 