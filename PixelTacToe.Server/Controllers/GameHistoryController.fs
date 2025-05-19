namespace PixelTacToe.Server.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open PixelTacToe.Server.Services
open PixelTacToe.Server.Models

[<ApiController>]
[<Route("api/[controller]")>]
type GameHistoryController(logger: ILogger<GameHistoryController>, gameRepository: GameRepository, playerRepository: PlayerRepository) =
    inherit ControllerBase()
    
    [<HttpGet("player/{playerId}")>]
    member self.GetPlayerGameHistory(playerId: string, [<FromQuery>] page: int, [<FromQuery>] pageSize: int) =
        task {
            try
                let actualPage = if page <= 0 then 1 else page
                let actualPageSize = if pageSize <= 0 || pageSize > 50 then 10 else pageSize
                
                let! result = gameRepository.GetPlayerGameHistoryAsync(playerId, actualPageSize, actualPage)
                
                match result with
                | Ok games ->
                    let gameResults = 
                        games |> Seq.map (fun g -> 
                            {|
                                id = g.Id
                                startedAt = g.StartedAt
                                endedAt = g.EndedAt
                                opponent = 
                                    if g.Player1Id = playerId then g.Player2Id
                                    else g.Player1Id
                                result = 
                                    match g.WinnerPlayerId with
                                    | Some winnerId when winnerId = playerId -> "win"
                                    | Some _ -> "loss"
                                    | None -> "draw"
                            |}
                        )
                    
                    return self.Ok(gameResults) :> IActionResult
                | Error message ->
                    logger.LogError("Failed to retrieve game history: {message}", message)
                    return self.StatusCode(500, {| message = "Failed to retrieve game history" |}) :> IActionResult
            with
            | ex ->
                logger.LogError(ex, "Error retrieving game history")
                return self.StatusCode(500, {| message = "Internal server error" |}) :> IActionResult
        }
        
    [<HttpGet("{gameId}/replay")>]
    member self.GetGameReplay(gameId: string) =
        task {
            try
                let! result = gameRepository.GetGameReplayAsync(gameId)
                
                match result with
                | Ok moves ->
                    let moveHistory = 
                        moves |> Seq.map (fun m -> 
                            {|
                                moveNumber = m.MoveNumber
                                playerId = m.PlayerId
                                row = m.Row
                                col = m.Col
                                timestamp = m.Timestamp
                            |}
                        )
                    
                    return self.Ok(moveHistory) :> IActionResult
                | Error message ->
                    logger.LogError("Failed to retrieve game replay: {message}", message)
                    return self.StatusCode(500, {| message = "Failed to retrieve game replay" |}) :> IActionResult
            with
            | ex ->
                logger.LogError(ex, "Error retrieving game replay")
                return self.StatusCode(500, {| message = "Internal server error" |}) :> IActionResult
        } 