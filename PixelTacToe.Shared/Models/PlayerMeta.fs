module PixelTacToe.Shared.PlayerModel

open System.Text.Json.Serialization

[<CLIMutable>]
type PlayerMeta =
  { [<JsonPropertyName("playerName")>] PlayerName : string
    [<JsonPropertyName("imageUrl")  >] ImageUrl   : string
    [<JsonPropertyName("inGame")    >] InGame     : bool
    [<JsonPropertyName("matchRequestBy")>]
    MatchRequestBy : string list
    CurrentGameId  : string option}