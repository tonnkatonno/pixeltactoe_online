namespace PixelTacToe.Server.Controllers

open System
open System.IO
open System.Text.Json
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open PixelTacToe.Server.Helpers.Shared
open PixelTacToe.Shared.PlayerModel

[<ApiController>]
[<Route("api/[controller]")>]
type PlayersController(cfg : IConfiguration, log : ILogger<PlayersController>) =
    inherit ControllerBase()

    // ---------- 1)  GET /api/players/{myId}/matchrequests ----------
    [<HttpGet("{myId}/matchrequests")>]
    member _.GetMatchRequests(myId : string) : IActionResult =
        let jsonPath =
            Path.Combine(UploadHelpers.getUploadPath cfg, $"{myId}.json")
        if not (File.Exists jsonPath) then
            base.NotFound $"Player {myId} not found"
        else
            let opts = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
            use fs = File.OpenRead jsonPath
            let meta = JsonSerializer.Deserialize<PlayerMeta>(fs, opts)
            base.Ok meta.MatchRequestBy

    // 2)  POST /api/players/{targetId}/matchrequest
    [<HttpPost("{targetId}/matchrequest")>]
    member this.PostMatchRequest(targetId : string,
                                 [<FromBody>] requesterId : string)
                                 : Task<IActionResult> = task {

        let controllerBase = this :> ControllerBase
        let jsonPath = Path.Combine(UploadHelpers.getUploadPath cfg, $"{targetId}.json")

        if not (File.Exists jsonPath) then
            return controllerBase.NotFound($"Player {targetId} not found") :> IActionResult
        else
            try
                use fs = new FileStream(jsonPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)
                let opts = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
                let meta = JsonSerializer.Deserialize<PlayerMeta>(fs, opts)

                if meta.InGame then
                    return controllerBase.BadRequest("Player already in game") :> IActionResult
                else
                    let already = meta.MatchRequestBy |> List.exists ((=) requesterId)
                    if already then
                        return controllerBase.Ok() :> IActionResult
                    else
                        let updated =
                            { meta with MatchRequestBy = requesterId :: meta.MatchRequestBy }

                        fs.SetLength 0L
                        fs.Position <- 0L
                        JsonSerializer.Serialize(fs, updated, opts)
                        do! fs.FlushAsync()

                        return controllerBase.Ok() :> IActionResult

            with ex ->
                log.LogError(ex, "PostMatchRequest error")
                return controllerBase.StatusCode(500, "PostMatchRequest failed") :> IActionResult
    }

    // 3) POST /api/players/{targetId}/acceptmatchrequest
    [<HttpPost("{targetId}/acceptmatchrequest")>]
    member _.AcceptMatchRequest
            (targetId : string,
             [<FromBody>] accepterId : string)
            : IActionResult =
        
        let uploads  = UploadHelpers.getUploadPath cfg
        let gamesDir =
            let g = Path.Combine(uploads, "..", "games") |> Path.GetFullPath
            if not (Directory.Exists g) then Directory.CreateDirectory g |> ignore
            g

        let targetJson   = Path.Combine(uploads, $"{targetId}.json")
        let accepterJson = Path.Combine(uploads, $"{accepterId}.json")
        let opts = JsonSerializerOptions(PropertyNameCaseInsensitive = true)

        if not (File.Exists targetJson && File.Exists accepterJson) then
            base.NotFound("One or both players not found")
        else
            use tgtFs = new FileStream(targetJson,   FileMode.Open, FileAccess.ReadWrite, FileShare.None)
            use accFs = new FileStream(accepterJson, FileMode.Open, FileAccess.ReadWrite, FileShare.None)

            let tgtMeta = JsonSerializer.Deserialize<PlayerMeta>(tgtFs, opts)
            let accMeta = JsonSerializer.Deserialize<PlayerMeta>(accFs, opts)

            if tgtMeta.InGame || accMeta.InGame then
                base.BadRequest("One of the players is already in a game")
            elif not (List.exists ((=) accepterId) tgtMeta.MatchRequestBy) then
                base.BadRequest("No pending match request from this player")
            else
                let gameId  = Guid.NewGuid().ToString()
                let gameObj =
                    {| gameId     = gameId
                       player1Id  = targetId
                       player1Img = tgtMeta.ImageUrl
                       player2Id  = accepterId
                       player2Img = accMeta.ImageUrl
                       state      = {| board      = [||]
                                       nextPlayer = targetId
                                       finished   = false
                                       winner     = "" |} |}

                let gamePath = Path.Combine(gamesDir, $"{gameId}.json")
                File.WriteAllText(gamePath, JsonSerializer.Serialize(gameObj, opts))

                let newTgt =
                    { tgtMeta with
                        InGame         = true
                        MatchRequestBy = tgtMeta.MatchRequestBy |> List.filter ((<>) accepterId)
                        CurrentGameId  = Some gameId }

                let newAcc =
                    { accMeta with
                        InGame        = true
                        CurrentGameId = Some gameId }

                let writeBack (fs : FileStream) meta =
                    fs.SetLength 0L
                    fs.Position <- 0L
                    JsonSerializer.Serialize(fs, meta, opts)
                    fs.Flush()

                writeBack tgtFs newTgt
                writeBack accFs newAcc

                log.LogInformation $"Game {gameId} created between {targetId} and {accepterId}"
                base.Ok {| gameId = gameId |}

        
    // 4)  GET /api/players/lobby
    [<HttpGet("players/lobby")>]
    member _.GetPlayersInLobby() : IActionResult =
        let uploads   =  PixelTacToe.Server.Helpers.Shared.UploadHelpers.getUploadPath cfg
        let jsonFiles = Directory.EnumerateFiles(uploads, "*.json")
        let opts      = JsonSerializerOptions(PropertyNameCaseInsensitive = true)

        let players =
            jsonFiles
            |> Seq.choose (fun path ->
                try
                    use fs = File.OpenRead path
                    let p = JsonSerializer.Deserialize<PixelTacToe.Shared.PlayerModel.PlayerMeta>(fs, opts)
                    if not p.InGame then
                         Some {| id       = Path.GetFileNameWithoutExtension path
                                 name     = p.PlayerName
                                 imageUrl = p.ImageUrl |}
                    else None
                with _ -> None)
            |> Seq.toList
        base.Ok players
        

    // 5)  GET /api/players/leaderboard
    [<HttpGet("players/leaderboard")>]
    member _.Leaderboard() : IActionResult =
        let uploads = UploadHelpers.getUploadPath cfg
        let opts    = JsonSerializerOptions(PropertyNameCaseInsensitive = true)

        let statsPath   pid = Path.Combine(uploads, $"{pid}.stats.json")
        let metaPath    pid = Path.Combine(uploads, $"{pid}.json")

        let leaders =
            Directory.EnumerateFiles(uploads, "*.json")
            |> Seq.choose (fun p ->
                let pid = Path.GetFileNameWithoutExtension p
                if pid.EndsWith(".stats") then None
                else
                    try
                        use ms = File.OpenRead(metaPath pid)
                        let meta = JsonSerializer.Deserialize<PlayerMeta>(ms, opts)

                        let st =
                            let sp = statsPath pid
                            if File.Exists sp then
                                use ss = File.OpenRead sp
                                JsonSerializer.Deserialize<PlayerStats>(ss, opts)
                            else PlayerStats.Zero

                        Some {| id = pid; name = meta.PlayerName; imageUrl = meta.ImageUrl
                                wins = st.Wins; losses = st.Losses; draws = st.Draws |}
                    with _ -> None)

            |> Seq.sortByDescending (fun p -> (p.wins - p.losses, p.wins, p.draws))
            |> Seq.truncate 20
            |> Seq.toList

        base.Ok(leaders) :> IActionResult

