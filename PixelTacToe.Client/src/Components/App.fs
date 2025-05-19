namespace PixelTacToe.Client.Components

open System
open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html
open PixelTacToe.Shared.Models
open PixelTacToe.Client.Services

/// Main application component
[<JavaScript>]
module App =
    // Initialize WebSocket connection based on the current window location
    let private initializeConnection() =
        let baseUrl = JS.Window.Location.Origin
        let wsUrl = WebSocketService.getWebSocketUrl(baseUrl)
        
        JS.Console.Log("Connecting to WebSocket server at: " + wsUrl)
        
        let connected = WebSocketService.connect(wsUrl)
        GameService.isConnected.Value <- connected
        
        if connected then
            JS.Console.Log("Connected to WebSocket server")
        else
            JS.Console.Error("Failed to connect to WebSocket server")
    
    // Render the connection status indicator
    let private renderConnectionStatus() =
        Doc.BindView (fun isConnected ->
            let (statusText, statusClass) = 
                if isConnected then ("Connected", "connected")
                else ("Disconnected", "disconnected")
                
            div [attr.class (sprintf "connection-status %s" statusClass)] [
                span [] [text statusText]
            ]
        ) GameService.isConnected.View
    
    // Render the main application content based on game state
    let private renderContent() =
        Doc.BindView (fun gameState ->
            match gameState with
            | GameService.NoGame ->
                // Show initial setup screen
                div [attr.class "welcome-screen"] [
                    h1 [] [text "Welcome to Pixel Tic-Tac-Toe"]
                    p [] [text "Please enter your name to join the lobby"]
                    
                    div [attr.class "name-entry-form"] [
                        div [attr.class "form-group"] [
                            label [attr.for' "playerNameInput"] [text "Your Name:"]
                            input [
                                attr.id "playerNameInput"
                                attr.type' "text"
                                attr.class "form-control"
                                attr.placeholder "Enter your name"
                                Attr.Value GameService.playerName.View
                                on.input (fun e ->
                                    let input = e.Target :?> Dom.HTMLInputElement
                                    GameService.playerName.Value <- input.Value
                                )
                            ]
                        ]
                        
                        button [
                            attr.class "btn btn-primary"
                            on.click (fun _ ->
                                if not (String.IsNullOrWhiteSpace(GameService.playerName.Value)) then
                                    // Generate a random player ID
                                    let playerId = System.Guid.NewGuid().ToString()
                                    
                                    // Get current image URL
                                    let imageUrl = ImageUploadService.getCurrentImageUrl()
                                    
                                    // Set player identity and update game state
                                    GameService.setPlayerIdentity playerId GameService.playerName.Value imageUrl
                                    GameService.gameStateVar.Value <- GameService.InLobby
                                    
                                    // Join lobby
                                    WebSocketService.sendMessage(
                                        ClientMessage.JoinLobby(playerId, GameService.playerName.Value, imageUrl)
                                    ) |> ignore
                            )
                            Attr.DynamicPred "disabled" (
                                GameService.playerName.View
                                |> View.Map String.IsNullOrWhiteSpace
                            )
                        ] [text "Join Lobby"]
                    ]
                ]
                
            | GameService.InLobby | GameService.Matchmaking -> 
                // Show the lobby screen
                Lobby.render()
                
            | GameService.InGame _ | GameService.GameOver _ -> 
                // Show the game board
                GameBoard.render()
                
        ) GameService.gameStateVar.View
    
    // Main render function for the application
    let render() =
        // Initialize the connection when the app starts
        initializeConnection()
        
        // Render the application
        div [attr.class "app-container"] [
            div [attr.class "app-header"] [
                h1 [attr.class "app-title"] [text "Pixel Tic-Tac-Toe"]
                renderConnectionStatus()
            ]
            
            div [attr.class "app-content"] [
                renderContent()
            ]
            
            div [attr.class "app-footer"] [
                p [] [text "© 2023 Pixel Tic-Tac-Toe Game"]
            ]
        ]
        
    // Start the application (for backward compatibility with older code)
    let start() =
        // This is kept for compatibility but no longer does anything
        () 