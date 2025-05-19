namespace PixelTacToe.Client.Services

open WebSharper
open WebSharper.UI
open WebSharper.UI.Client
open PixelTacToe.Shared.Models

[<JavaScript>]
module GameStateService =
    // Define local game state
    type GameState = {
        GameId: string
        Board: PlayerMarker option[,]
        CurrentTurnPlayerId: string
        Player1: Player
        Player2: Player
        IsGameOver: bool
        WinnerPlayerId: string option
    }
    
    // Create a wrapper around Var to provide more convenient access
    type GameStateVar = {
        Var: Var<GameState option>
        mutable Current: GameState option
    }
    
    // Get current image from the image service
    let getCurrentImageUrl() =
        ImageUploadService.getCurrentImageUrl() |> Option.defaultValue "images/default-avatar.png"
    
    // Create a new game state variable with convenience methods
    let CreateGameStateVar() =
        let v = Var.Create<GameState option>(None)
        
        let stateVar = {
            Var = v
            Current = None
        }
        
        // Listen for changes
        v.View |> View.Sink (fun state ->
            stateVar.Current <- state
        )
        
        // Return the wrapper
        stateVar
        
    // Extension method to update the game state
    type GameStateVar with
        member this.Update(f: GameState option -> GameState option) =
            // Use the current value to calculate a new one
            let newVal = f this.Current
            // Update the Var
            this.Var.Value <- newVal
            // Keep our Current property in sync
            this.Current <- newVal
        
        member this.Set(newState: GameState option) =
            // Set the Var directly
            this.Var.Value <- newState
            // Keep our Current property in sync
            this.Current <- newState 