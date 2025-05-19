namespace PixelTacToe.Client.Services

open System
open WebSharper
open WebSharper.UI
open WebSharper.JavaScript
open WebSharper.JavaScript.Console
open PixelTacToe.Shared.Models
open PixelTacToe.Shared.Constants
open PixelTacToe.Client.Services.WebSocketService
open PixelTacToe.Client.Services.ImageUploadService
open PixelTacToe.Client.Models

/// Service for managing client-side game state
[<JavaScript>]
module GameService =
    type GameState =
        | NoGame
        | InLobby
        | Matchmaking
        | InGame of GameData
        | GameOver of GameData * GameOutcome
    
    and GameData = {
        GameId: string
        Player: Player
        Opponent: Player
        CurrentBoard: string[,]
        IsPlayerTurn: bool
    }
    
    and GameOutcome =
        | Win
        | Loss
        | Draw
        | Abandoned

    // UI State 
    let playerName = Var.Create ""
    let playerId = Var.Create ""
    let opponentName = Var.Create ""
    let opponentImageUrl = Var.Create ""
    let playerCount = Var.Create 0
    let isSearching = Var.Create false
    let gameId = Var.Create ""
    let isMyTurn = Var.Create false
    let marker = Var.Create PlayerMarker.X
    let gameStatus = Var.Create "Enter your name and upload an image to start"
    
    let board : Var<Option<PlayerMarker>[,]> = Var.Create(Array2D.create GameConstants.BoardSize GameConstants.BoardSize None)
    let uploadedImageUrl = Var.Create<Option<string>>(None)
    let gameActive = Var.Create false
    let nameEntered = Var.Create false
    let isConnected = Var.Create false
    
    // Game state var
    let gameStateVar = Var.Create<GameState>(NoGame)
    
    // Internal state tracking for the game board
    let mutable private gameBoard: string[,] = Array2D.create 3 3 ""
    
    // Initialize with a default image
    do
        uploadedImageUrl.Value <- Some GameConstants.DefaultPlayerImage
    
    // Reset game state for a new game
    let resetGameState() =
        opponentName.Value <- ""
        opponentImageUrl.Value <- ""
        isSearching.Value <- false
        gameId.Value <- ""
        isMyTurn.Value <- false
        board.Value <- Array2D.create GameConstants.BoardSize GameConstants.BoardSize None
        gameActive.Value <- false
        gameStatus.Value <- "Click 'Find Match' to play"
    
    // Handle server message
    let handleServerMessage (message: ServerMessage) =
        match message with
        | PlayerJoined player ->
            playerId.Value <- player.Id
            playerName.Value <- player.Name
            nameEntered.Value <- true
            gameStatus.Value <- "You've joined the lobby. Click 'Find Match' to play."
        
        | PlayerCount count ->
            playerCount.Value <- count
        
        | GameStarted (gId, opponent, startingPlayerId) ->
            gameId.Value <- gId
            opponentName.Value <- opponent.Name
            opponentImageUrl.Value <- opponent.ImageUrl
            isSearching.Value <- false
            gameActive.Value <- true
            board.Value <- Array2D.create GameConstants.BoardSize GameConstants.BoardSize None
            
            isMyTurn.Value <- (startingPlayerId = playerId.Value)
            marker.Value <- if isMyTurn.Value then PlayerMarker.X else PlayerMarker.O
            
            gameStatus.Value <- 
                if isMyTurn.Value then 
                    "Your turn" 
                else 
                    "Waiting for " + opponent.Name + " to move"
        
        | MoveMade (gId, pId, row, col) ->
            if gId = gameId.Value then
                // Update the board
                let newBoard = Array2D.copy board.Value
                let playerMark = 
                    if pId = playerId.Value then marker.Value 
                    else (if marker.Value = PlayerMarker.X then PlayerMarker.O else PlayerMarker.X)
                newBoard.[row, col] <- Some playerMark
                board.Value <- newBoard
                
                // Toggle turn
                if pId <> playerId.Value then
                    isMyTurn.Value <- true
                    gameStatus.Value <- "Your turn"
                else
                    isMyTurn.Value <- false
                    gameStatus.Value <- "Waiting for " + opponentName.Value + " to move"
        
        | GameOver (gId, winnerId) ->
            if gId = gameId.Value then
                gameActive.Value <- false
                
                match winnerId with
                | Some id when id = playerId.Value ->
                    gameStatus.Value <- "You win! Click 'Find New Match' to play again."
                | Some _ ->
                    gameStatus.Value <- opponentName.Value + " wins! Click 'Find New Match' to play again."
                | None ->
                    gameStatus.Value <- "It's a draw! Click 'Find New Match' to play again."
        
        | Error message ->
            gameStatus.Value <- "Error: " + message
            error("Server error: " + message)
    
    // Join the game lobby with a name and avatar
    let joinLobby (name: string) (imageUrl: string) =
        playerName.Value <- name
        uploadedImageUrl.Value <- Some (if String.IsNullOrEmpty(imageUrl) then GameConstants.DefaultPlayerImage else imageUrl)
        
        // Generate player ID if not set
        if String.IsNullOrEmpty(playerId.Value) then
            playerId.Value <- Guid.NewGuid().ToString()
            
        // Prepare client message
        let joinMsg = ClientMessage.JoinLobby(playerId.Value, name, imageUrl)
        
        // Send via WebSocket service
        if sendMessage(joinMsg) then
            gameStatus.Value <- "Joining lobby..."
        else
            gameStatus.Value <- "Failed to join. Please check your connection."
    
    // Start matchmaking
    let findMatch() =
        if String.IsNullOrEmpty(playerId.Value) then
            gameStatus.Value <- "You must join the lobby first."
        else
            let message = ClientMessage.StartMatchmaking(playerId.Value)
            if sendMessage(message) then
                isSearching.Value <- true
                gameStatus.Value <- "Looking for an opponent..."
            else
                gameStatus.Value <- "Failed to start matchmaking. Please check your connection."
    
    // Cancel matchmaking
    let cancelMatchmaking() =
        let message = ClientMessage.CancelMatchmaking
        if sendMessage(message) then
            isSearching.Value <- false
            gameStatus.Value <- "Matchmaking cancelled."
        else
            gameStatus.Value <- "Failed to cancel matchmaking. Please check your connection."
    
    // Make a move on the board
    let makeMove (row: int) (col: int) =
        if gameActive.Value && isMyTurn.Value && not (String.IsNullOrEmpty(gameId.Value)) then
            let message = ClientMessage.MakeMove(gameId.Value, playerId.Value, row, col)
            
            if sendMessage(message) then
                // UI is updated when we receive the MoveMade message back from the server
                // But we could also update locally for responsiveness
                ()
            else
                gameStatus.Value <- "Failed to make move. Please check your connection."
    
    // Leave the current game
    let leaveGame() =
        if gameActive.Value then
            let message = ClientMessage.LeaveGame(gameId.Value)
            if sendMessage(message) then
                gameActive.Value <- false
                gameStatus.Value <- "Game ended. Click 'Find Match' to play again."
                board.Value <- Array2D.create GameConstants.BoardSize GameConstants.BoardSize None
            else
                gameStatus.Value <- "Failed to leave game. Please check your connection."
    
    // Connect to the WebSocket server
    let connectToServer (serverBaseUrl: string) =
        let wsUrl = getWebSocketUrl serverBaseUrl
        
        // Add listener for WebSocket events
        addEventListener (fun event ->
            match event with
            | Connected ->
                isConnected.Value <- true
                gameStatus.Value <- "Connected to server. Enter your name and click 'Join Game' to play."
                
            | Disconnected ->
                isConnected.Value <- false
                gameActive.Value <- false
                gameStatus.Value <- "Connection lost. Please refresh to reconnect."
                
            | MessageReceived serverMsg ->
                handleServerMessage serverMsg
                
            | Error errMsg ->
                gameStatus.Value <- "Connection error: " + errMsg
        )
        
        // Connect to WebSocket server
        let success = connect wsUrl
        
        if not success then
            gameStatus.Value <- "Failed to connect to the game server. Please refresh and try again."
            
        success 

    // Get current game state
    let getGameState() = gameStateVar.Value
    
    // Set player identity
    let setPlayerIdentity (id: string) (name: string) (image: string) =
        playerId.Value <- id
        playerName.Value <- name
        uploadedImageUrl.Value <- Some (if String.IsNullOrEmpty(image) then GameConstants.DefaultPlayerImage else image)
    
    // Get player info
    let getPlayerId() = playerId.Value
    let getPlayerName() = playerName.Value
    let getPlayerImageUrl() = uploadedImageUrl.Value
    
    // Handle WebSocket events
    let handleWebSocketEvent (event: WebSocketService.WebSocketEvent) =
        match event with
        | WebSocketService.Connected ->
            // Send join lobby message on connect
            sendMessage(ClientMessage.JoinLobby(playerId.Value, playerName.Value, uploadedImageUrl.Value.Value)) |> ignore
        
        | WebSocketService.MessageReceived(ServerMessage.GameStarted(gameId, opponent, startingPlayerId)) ->
            // Reset game board
            gameBoard <- Array2D.create 3 3 ""
            
            // Create player object
            let player = {
                Id = playerId.Value
                Name = playerName.Value
                ImageUrl = uploadedImageUrl.Value.Value
                IsSearching = false
            }
            
            // Create game data
            let gameData = {
                GameId = gameId
                Player = player
                Opponent = opponent
                CurrentBoard = gameBoard
                IsPlayerTurn = startingPlayerId = playerId.Value
            }
            
            // Update game state
            gameStateVar.Value <- InGame gameData
            
        | WebSocketService.MessageReceived(ServerMessage.MoveMade(gameId, movePlayerId, row, col)) ->
            match gameStateVar.Value with
            | InGame gameData when gameData.GameId = gameId ->
                // Update the game board
                gameBoard.[row, col] <- if movePlayerId = playerId.Value then "X" else "O"
                
                // Create updated game data with the new board and turn state
                let updatedGameData = {
                    gameData with
                        CurrentBoard = gameBoard
                        IsPlayerTurn = movePlayerId <> playerId.Value // Toggle turn
                }
                
                // Update the game state
                gameStateVar.Value <- InGame updatedGameData
                
            | _ -> () // Ignore if not in this game
            
        | WebSocketService.MessageReceived(ServerMessage.GameOver(gameId, winnerIdOpt)) ->
            match gameStateVar.Value with
            | InGame gameData when gameData.GameId = gameId ->
                // Determine game outcome
                let outcome =
                    match winnerIdOpt with
                    | Some winnerId when winnerId = playerId.Value -> Win
                    | Some _ -> Loss
                    | None -> Draw
                
                // Update game state to game over
                gameStateVar.Value <- GameOver(gameData, outcome)
                
            | _ -> () // Ignore if not in this game
            
        | _ -> () // Ignore other events
    
    // Initialize game service
    let initialize() =
        // Generate a random player ID if not set
        if String.IsNullOrEmpty(playerId.Value) then
            let randomId = System.Guid.NewGuid().ToString()
            playerId.Value <- randomId
            
        // Subscribe to WebSocket events
        addEventListener handleWebSocketEvent
        
    // Start matchmaking
    let startMatchmaking() =
        match gameStateVar.Value with
        | InLobby ->
            // Send start matchmaking message
            if sendMessage(ClientMessage.StartMatchmaking(playerId.Value)) then
                gameStateVar.Value <- Matchmaking
                true
            else
                false
        | _ -> false
    
    // Cancel matchmaking
    let cancelMatchmaking() =
        match gameStateVar.Value with
        | Matchmaking ->
            // Send cancel matchmaking message
            if sendMessage(ClientMessage.CancelMatchmaking) then
                gameStateVar.Value <- InLobby
                true
            else
                false
        | _ -> false
    
    // Make a move on the game board
    let makeMove (row: int) (col: int) =
        match gameStateVar.Value with
        | InGame gameData when gameData.IsPlayerTurn && gameData.CurrentBoard.[row, col] = "" ->
            // Send make move message
            sendMessage(
                ClientMessage.MakeMove(gameData.GameId, playerId.Value, row, col)
            ) |> ignore
            true
        | _ -> false
    
    // Leave the current game
    let leaveGame() =
        match gameStateVar.Value with
        | InGame gameData ->
            // Send leave game message
            sendMessage(ClientMessage.LeaveGame(gameData.GameId)) |> ignore
            gameStateVar.Value <- InLobby
            true
        | GameOver(gameData, _) ->
            gameStateVar.Value <- InLobby
            true
        | _ -> false
    
    // Check if a position on the board is empty
    let isEmptyPosition (row: int) (col: int) =
        match gameStateVar.Value with
        | InGame gameData -> gameData.CurrentBoard.[row, col] = ""
        | _ -> false 