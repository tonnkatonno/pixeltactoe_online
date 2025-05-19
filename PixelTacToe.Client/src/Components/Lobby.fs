namespace PixelTacToe.Client.Components

open System
open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html
open PixelTacToe.Shared.Models
open PixelTacToe.Client.Services

/// Component for the lobby screen
[<JavaScript>]
module Lobby =
    // Handle player name input
    let private handleNameInput (e: Dom.Event) =
        let input = As<Dom.HTMLInputElement>(e.Target)
        let value = input.Value
        
        // Update player name in service
        GameService.playerName.Value <- value
        
        // Handle empty names or valid names
        if String.IsNullOrWhiteSpace(value) then
            GameService.nameEntered.Value <- false
        else
            GameService.nameEntered.Value <- true
    
    // Handle file input change for avatar upload
    let private handleFileInputChange (e: Dom.Event) =
        let input = e.Target
        ImageUploadService.uploadImageFromInput(input) |> ignore
    
    // Handle start matchmaking button click
    let private handleStartMatchmaking (e: Dom.MouseEvent) =
        GameService.startMatchmaking() |> ignore
    
    // Handle cancel matchmaking button click
    let private handleCancelMatchmaking (e: Dom.MouseEvent) =
        GameService.cancelMatchmaking() |> ignore
    
    // Render player image with upload option
    let private renderPlayerImage () =
        let imageUrlView = 
            View.Map (fun url -> 
                match url with
                | Some url -> url
                | None -> ImageUploadService.getDefaultImageUrl()
            ) GameService.uploadedImageUrl.View
        
        div [attr.classDyn "player-image-container"] [
            img [
                attr.srcDyn imageUrlView
                attr.altDyn "Player Avatar"
                attr.classDyn "player-avatar"
            ]
            label [attr.classDyn "image-upload-label"] [
                text "Choose Avatar"
                input [
                    attr.typeDyn "file"
                    attr.classDyn "image-upload-input"
                    attr.accept "image/*"
                    on.change handleFileInputChange
                ]
            ]
        ]
    
    // Render player name input form
    let private renderNameInput () =
        let nameEnteredView = GameService.nameEntered.View
        
        div [attr.classDyn "name-input-container"] [
            h3 [] [text "Enter Your Name"]
            div [attr.classDyn "form-group"] [
                label [attr.forDyn "playerName"] [text "Your Name:"]
                input [
                    attr.idDyn "playerName"
                    attr.typeDyn "text"
                    attr.classDyn "form-control"
                    attr.placeholderDyn "Enter your name"
                    attr.valueDyn GameService.playerName.View
                    on.input handleNameInput
                ]
            ]
        ]
    
    // Render matchmaking controls
    let private renderMatchmakingControls () =
        // View that determines if matchmaking is active
        let isMatchmakingView = 
            GameService.gameStateVar.View 
            |> View.Map (function 
                | GameService.Matchmaking -> true 
                | _ -> false)
        
        Doc.BindView (fun isMatchmaking ->
            if isMatchmaking then
                div [attr.classDyn "matchmaking-controls"] [
                    div [attr.classDyn "matchmaking-status"] [
                        text "Looking for an opponent..."
                    ]
                    button [
                        attr.classDyn "btn btn-danger"
                        on.click handleCancelMatchmaking
                    ] [text "Cancel"]
                ]
            else
                div [attr.classDyn "matchmaking-controls"] [
                    button [
                        attr.classDyn "btn btn-primary"
                        on.click handleStartMatchmaking
                        attr.disabledDyn (
                            GameService.nameEntered.View 
                            |> View.Map not)
                    ] [text "Find Match"]
                ]
        ) isMatchmakingView
    
    // Render lobby players list
    let private renderPlayersList (players: seq<Player>) =
        div [attr.classDyn "players-list"] [
            h3 [] [text "Players Online"]
            ul [attr.classDyn "list-group"] [
                for player in players do
                    li [attr.classDyn "list-group-item"] [
                        div [attr.classDyn "player-list-item"] [
                            img [
                                attr.src player.ImageUrl
                                attr.alt (player.Name + "'s avatar")
                                attr.classDyn "player-list-avatar"
                            ]
                            span [attr.classDyn "player-list-name"] [text player.Name]
                            if player.IsSearching then
                                span [attr.classDyn "player-searching-badge"] [text "Searching..."]
                        ]
                    ]
            ]
        ]
    
    // Main render function
    let render () =
        div [attr.classDyn "lobby-container"] [
            h1 [attr.classDyn "lobby-title"] [text "Pixel Tic-Tac-Toe"]
            
            div [attr.classDyn "player-setup"] [
                renderPlayerImage()
                renderNameInput()
                renderMatchmakingControls()
            ]
            
            // Player count would be rendered here based on data from server
            Doc.BindView (fun count ->
                div [attr.classDyn "player-count"] [
                    text (sprintf "Players Online: %d" count)
                ]
            ) GameService.playerCount.View
            
            // Players list would be populated from server data
            Doc.BindView (fun players ->
                renderPlayersList players
            ) GameService.players.View
        ] 