namespace PixelTacToe.Server.Helpers.Shared

open System.IO
open Microsoft.Extensions.Configuration

type PlayerStats =
    { Wins   : int
      Losses : int
      Draws  : int }
    static member Zero = { Wins = 0; Losses = 0; Draws = 0 }

[<RequireQualifiedAccess>]
module UploadHelpers =

    [<Literal>]
    let MaxFileSize = 2L * 1024L * 1024L

    let allowedMimeTypes =
        set [ "image/jpeg"; "image/png"; "image/gif"
              "image/webp"; "image/svg+xml" ]

    let getUploadPath (config : IConfiguration) =
        let path = config.GetValue<string>("UploadSettings:Path", "wwwroot/uploads")
        if not (Directory.Exists path) then Directory.CreateDirectory path |> ignore
        path

    let getGamesDir (config : IConfiguration) =
        let uploads = getUploadPath config
        let g = Path.Combine(uploads, "..", "games") |> Path.GetFullPath
        if not (Directory.Exists g) then Directory.CreateDirectory g |> ignore
        g
