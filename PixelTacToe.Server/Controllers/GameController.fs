namespace PixelTacToe.Server.Controllers

open System.IO
open System.Text.Json
open System.Text.Json.Nodes
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open PixelTacToe.Server.Helpers.Shared
open PixelTacToe.Shared.PlayerModel     

[<CLIMutable>]
type MoveDto =
    { playerId : string
      cell     : int }

[<ApiController>]
[<Route("api/[controller]")>]
type GameController(cfg : IConfiguration, log : ILogger<GameController>) =
    inherit ControllerBase()

    // ---------------------------------------------------------------------
    // 1) GET  /api/game/{gameId}  – teljes játékállapot JSON visszaadása
    // ---------------------------------------------------------------------
    [<HttpGet("{gameId}")>]
    member _.GetGame(gameId : string) : IActionResult =
        let gamePath = Path.Combine(UploadHelpers.getGamesDir cfg, $"{gameId}.json")
        if not (File.Exists gamePath) then
            base.NotFound("Game not found")
        else
            let json = File.ReadAllText gamePath
            base.Content(json, "application/json")

    // ---------------------------------------------------------------------
    // 2) POST /api/game/{gameId}/move  – lépés beküldése
    // ---------------------------------------------------------------------
    /// <summary>
    /// A kliens egyetlen lépését írja le (játékos ID + mezőindex).
    /// </summary>
    /// <remarks>
    /// A mezőindexek elrendezése a 3×3‑as táblán:
    /// <code>
    ///  0 | 1 | 2
    /// ---+---+---
    ///  3 | 4 | 5
    /// ---+---+---
    ///  6 | 7 | 8
    /// </code>
    /// </remarks>
    [<HttpPost("{gameId}/move")>]
    member _.PostMove(gameId : string, [<FromBody>] dto : MoveDto) : IActionResult =
        if dto.cell < 0 || dto.cell > 8 then
            base.BadRequest("Cell index must be between 0 and 8")
        else
            let gamePath = Path.Combine(UploadHelpers.getGamesDir cfg, $"{gameId}.json")
            if not (File.Exists gamePath) then
                base.NotFound("Game not found")
            else
                use fs = new FileStream(gamePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)
                let jsonObj = JsonNode.Parse(fs) :?> JsonObject

                let player1Id = jsonObj["player1Id"].GetValue<string>()
                let player2Id = jsonObj["player2Id"].GetValue<string>()

                if dto.playerId <> player1Id && dto.playerId <> player2Id then
                    base.BadRequest("You are not a participant of this game")
                else
                    let stateObj = jsonObj["state"] :?> JsonObject
                    let boardArr  =
                        match stateObj["board"] with
                        | :? JsonArray as ja when ja.Count = 9 -> ja
                        | :? JsonArray as ja ->
                            ja.Clear()
                            for _ in 0..8 do ja.Add(JsonValue.Create "")
                            ja
                        | null ->
                            let ja = JsonArray()
                            for _ in 0..8 do ja.Add(JsonValue.Create "")
                            stateObj["board"] <- ja
                            ja
                        | jsonNode -> failwith "Invalid board format"
                    let nextPlayer = stateObj["nextPlayer"].GetValue<string>()

                    if nextPlayer <> dto.playerId then
                        base.BadRequest("Not your turn")
                    elif boardArr[dto.cell].GetValue<string>() <> "" then
                        base.BadRequest("Cell already occupied")
                    else
                        // Jelölés: player1 -> "X", player2 -> "O"
                        let mark = if dto.playerId = player1Id then "X" else "O"
                        boardArr[dto.cell] <- JsonValue.Create mark

                        // --- nyert-e valaki? ---
                        let lines =
                            [| [|0;1;2|]; [|3;4;5|]; [|6;7;8|]   // sorok
                               [|0;3;6|]; [|1;4;7|]; [|2;5;8|]   // oszlopok
                               [|0;4;8|]; [|2;4;6|] |]           // átlók
                        let hasWon m =
                            lines |> Array.exists (fun ln -> ln |> Array.forall (fun i -> boardArr[i].GetValue<string>() = m))

                        let finished, winnerOpt =
                            if hasWon mark then true, Some dto.playerId
                            elif boardArr |> Seq.forall (fun j -> j.GetValue<string>() <> "") then true, None // döntetlen
                            else false, None

                        // állapot frissítése
                        stateObj["nextPlayer"] <- JsonValue.Create (if finished then "" else (if dto.playerId = player1Id then player2Id else player1Id))
                        if finished then
                            stateObj["finished"] <- JsonValue.Create true
                            match winnerOpt with
                            | Some w -> stateObj["winner"] <- JsonValue.Create w
                            | None   -> stateObj["winner"] <- JsonValue.Create "draw"

                        let uploads = UploadHelpers.getUploadPath cfg
                        let jsonOpts = JsonSerializerOptions(PropertyNameCaseInsensitive = true,
                                                             WriteIndented = true)

                        let updatePlayer pid winFlag =
                            // meta ↓
                            let metaPath = Path.Combine(uploads, $"{pid}.json")
                            if File.Exists metaPath then
                                let meta =
                                    use ms = File.OpenRead metaPath
                                    JsonSerializer.Deserialize<PlayerMeta>(ms, jsonOpts)
                                let newMeta = { meta with InGame = false; CurrentGameId = None }
                                File.WriteAllText(metaPath, JsonSerializer.Serialize(newMeta, jsonOpts))

                                // stat ↓
                                let statPath = Path.ChangeExtension(metaPath, ".stats.json")
                                let stats =
                                    if File.Exists statPath then
                                        use ss = File.OpenRead statPath
                                        JsonSerializer.Deserialize<PlayerStats>(ss, jsonOpts)
                                    else PlayerStats.Zero

                                let updated =
                                    match winFlag with
                                    | Some true  -> { stats with Wins   = stats.Wins   + 1 }
                                    | Some false -> { stats with Losses = stats.Losses + 1 }
                                    | None       -> { stats with Draws  = stats.Draws  + 1 }

                                File.WriteAllText(statPath, JsonSerializer.Serialize(updated, jsonOpts))

                        // ki-nyert?  (Some true / Some false / None)
                        let resFor pid =
                            match winnerOpt with
                            | Some w when w = pid -> Some true
                            | Some _              -> Some false
                            | None                -> None

                        updatePlayer player1Id (resFor player1Id)
                        updatePlayer player2Id (resFor player2Id)
                        
                        // visszaírás
                        fs.SetLength 0L
                        fs.Position <- 0L
                        JsonSerializer.Serialize(fs, jsonObj, JsonSerializerOptions(WriteIndented = true))
                        fs.Flush()

                        base.Content(jsonObj.ToJsonString(JsonSerializerOptions(WriteIndented = true)), "application/json")
