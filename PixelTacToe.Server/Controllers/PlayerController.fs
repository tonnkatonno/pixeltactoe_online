namespace PixelTacToe.Server.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open PixelTacToe.Server.Services
open PixelTacToe.Server.Models

[<ApiController>]
[<Route("api/[controller]")>]
type PlayerController(logger: ILogger<PlayerController>, playerRepository: PlayerRepository) =
    inherit ControllerBase()
    
    [<HttpGet("{id}")>]
    member self.GetPlayer(id: string) =
        task {
            try
                let! player = playerRepository.GetPlayerByIdAsync(id)
                
                match player with
                | Some p -> 
                    return self.Ok({|
                        id = p.Id
                        name = p.Name
                        imageUrl = p.ImageUrl
                        stats = {|
                            gamesPlayed = p.GamesPlayed
                            gamesWon = p.GamesWon
                            gamesDraw = p.GamesDraw
                            gamesLost = p.GamesPlayed - p.GamesWon - p.GamesDraw
                            winRate = if p.GamesPlayed > 0 then float p.GamesWon / float p.GamesPlayed * 100.0 else 0.0
                        |}
                    |}) :> IActionResult
                | None ->
                    return self.NotFound({| message = "Player not found" |}) :> IActionResult
            with
            | ex ->
                logger.LogError(ex, "Error retrieving player")
                return self.StatusCode(500, {| message = "Internal server error" |}) :> IActionResult
        } 