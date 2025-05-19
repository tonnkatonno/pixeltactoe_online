namespace PixelTacToe.Server.Controllers

open System
open System.IO
open System.Threading.Tasks
open System.Text.Json
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration

open PixelTacToe.Server.Helpers.Shared
open PixelTacToe.Shared.PlayerModel

[<CLIMutable>]
type CreatePlayerDto =
    { [<FromForm(Name = "name")>] Name : string
      [<FromForm(Name = "file")>] File : IFormFile }

[<ApiController>]
[<Route("api/[controller]")>]
type CreatePlayerController
        (logger : ILogger<CreatePlayerController>, cfg : IConfiguration) =

    inherit ControllerBase()

    let uploadDir ()      = UploadHelpers.getUploadPath cfg
    let maxFileSize       = UploadHelpers.MaxFileSize
    let allowedMimeTypes  = UploadHelpers.allowedMimeTypes

    // ---------- POST /api/createplayer/CreatePlayer ----------
    [<HttpPost("CreatePlayer")>]
    [<Consumes("multipart/form-data")>]
    [<RequestSizeLimit(UploadHelpers.MaxFileSize)>]
    member this.CreatePlayer
        ([<FromForm>] dto : CreatePlayerDto)
        : Task<IActionResult> = task {

        try
            match dto with
            | _ when String.IsNullOrWhiteSpace dto.Name ->
                return this.BadRequest("Name is required") :> _
            | _ when isNull dto.File ->
                return this.BadRequest("No file") :> _
            | _ when dto.File.Length = 0L ->
                return this.BadRequest("Empty file") :> _
            | _ when dto.File.Length > maxFileSize ->
                return this.BadRequest("File > 2 MB") :> _
            | _ when not (allowedMimeTypes.Contains dto.File.ContentType) ->
                return this.BadRequest("Invalid file type") :> _
            | _ ->
                let uniqueName = $"{Guid.NewGuid()}"
                let fileName   = $"{uniqueName}{Path.GetExtension dto.File.FileName}"
                let imgPath    = Path.Combine(uploadDir (), fileName)

                use fs = new FileStream(imgPath, FileMode.Create)
                do! dto.File.CopyToAsync fs

                let meta : PlayerMeta =
                    { PlayerName = dto.Name
                      ImageUrl   = $"/uploads/{fileName}"
                      InGame     = false
                      MatchRequestBy = []
                      CurrentGameId  = None}  

                let jsonOpts = JsonSerializerOptions(WriteIndented = true)
                let jsonPath = Path.ChangeExtension(imgPath, ".json")
                let json     = JsonSerializer.Serialize(meta, jsonOpts)
                do! File.WriteAllTextAsync(jsonPath, json)

                logger.LogInformation $"Player created: {dto.Name}, file {uniqueName}"
                return this.Ok {| url = meta.ImageUrl; createdPlayerId = uniqueName |} :> IActionResult


        with ex ->
            logger.LogError(ex, "CreatePlayer error")
            return this.StatusCode(StatusCodes.Status500InternalServerError,
                                   "CreatePlayer failed") :> _
    }
