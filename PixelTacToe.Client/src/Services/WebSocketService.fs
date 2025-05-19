namespace PixelTacToe.Client.Services

open System
open WebSharper
open WebSharper.JavaScript
open PixelTacToe.Shared.Models
open PixelTacToe.Shared.Constants

/// Service for handling WebSocket communication
[<JavaScript>]
module WebSocketService =
    // WebSocket event types
    type WebSocketEvent =
        | Connected
        | Disconnected
        | MessageReceived of ServerMessage
        | Error of message: string
    
    // Event handler type
    type WebSocketEventHandler = WebSocketEvent -> unit
    
    // State
    let mutable private socket: WebSharper.JavaScript.Dom.WebSocket option = None
    let mutable private isConnected = false
    let mutable private eventHandlers: ResizeArray<WebSocketEventHandler> = ResizeArray()
    
    // Register an event handler
    let addEventListener (handler: WebSocketEventHandler) =
        eventHandlers.Add(handler)
    
    // Remove an event handler
    let removeEventListener (handler: WebSocketEventHandler) =
        eventHandlers.RemoveAll(fun h -> System.Object.ReferenceEquals(h, handler)) |> ignore
    
    // Trigger an event
    let private triggerEvent (e: WebSocketEvent) =
        for handler in eventHandlers do
            try
                handler e
            with ex ->
                JS.Console.Error("Error in WebSocket event handler: " + ex.Message)
    
    // Convert client message to JSON
    let private messageToJson (message: ClientMessage) =
        let obj = JSObject()
        
        match message with
        | JoinLobby(playerId, playerName, imageUrl) ->
            obj?``type`` <- "JoinLobby"
            obj?playerId <- playerId
            obj?playerName <- playerName
            obj?imageUrl <- imageUrl
            
        | StartMatchmaking(playerId) ->
            obj?``type`` <- "StartMatchmaking"
            obj?playerId <- playerId
            
        | CancelMatchmaking ->
            obj?``type`` <- "CancelMatchmaking"
            
        | MakeMove(gameId, playerId, row, col) ->
            obj?``type`` <- "MakeMove"
            obj?gameId <- gameId
            obj?playerId <- playerId
            obj?row <- row
            obj?col <- col
            
        | LeaveGame(gameId) ->
            obj?``type`` <- "LeaveGame"
            obj?gameId <- gameId
            
        obj
    
    // Parse server message from JSON
    let private parseServerMessage (jsonStr: string) =
        try
            let msg = JSON.Parse(jsonStr)
            let messageType = msg?``type`` |> string
            
            match messageType with
            | "PlayerJoined" ->
                let player = {
                    Id = msg?player?Id |> string
                    Name = msg?player?Name |> string
                    ImageUrl = msg?player?ImageUrl |> string
                    IsSearching = msg?player?IsSearching |> bool
                }
                Some(ServerMessage.PlayerJoined(player))
                
            | "PlayerCount" ->
                let count = msg?count |> int
                Some(ServerMessage.PlayerCount(count))
                
            | "GameStarted" ->
                let gameId = msg?gameId |> string
                let opponent = {
                    Id = msg?opponent?Id |> string
                    Name = msg?opponent?Name |> string
                    ImageUrl = msg?opponent?ImageUrl |> string
                    IsSearching = msg?opponent?IsSearching |> bool
                }
                let startingPlayerId = msg?startingPlayerId |> string
                Some(ServerMessage.GameStarted(gameId, opponent, startingPlayerId))
                
            | "MoveMade" ->
                let gameId = msg?gameId |> string
                let playerId = msg?playerId |> string
                let row = msg?row |> int
                let col = msg?col |> int
                Some(ServerMessage.MoveMade(gameId, playerId, row, col))
                
            | "GameOver" ->
                let gameId = msg?gameId |> string
                let winnerId = 
                    if isNull msg?winnerPlayerId then
                        None
                    else
                        Some(msg?winnerPlayerId |> string)
                Some(ServerMessage.GameOver(gameId, winnerId))
                
            | "Error" ->
                let message = msg?message |> string
                Some(ServerMessage.Error(message))
                
            | _ ->
                JS.Console.Warn("Unknown message type: " + messageType)
                None
        with ex ->
            JS.Console.Error("Error parsing server message: " + ex.Message)
            None
    
    // Initialize the WebSocket service
    let initialize() =
        // Nothing to do yet
        ()
    
    // Connect to the WebSocket server
    let connect (serverUrl: string) =
        try
            // Close any existing connection
            match socket with
            | Some ws when ws.ReadyState <= 1 -> // 0 = connecting, 1 = open
                ws.Close()
                socket <- None
            | _ -> ()
            
            // Create new connection
            let ws = new WebSharper.JavaScript.Dom.WebSocket(serverUrl)
            
            let openHandler (e: Dom.Event) =
                isConnected <- true
                JS.Console.Log("WebSocket connection established")
                triggerEvent Connected
                
            let closeHandler (e: Dom.Event) =
                isConnected <- false
                socket <- None
                JS.Console.Log("WebSocket connection closed")
                triggerEvent Disconnected
                
            let errorHandler (e: Dom.Event) =
                JS.Console.Error("WebSocket error occurred")
                triggerEvent (Error "Connection error")
                
            let messageHandler (e: Dom.Event) =
                let messageEvent = e :?> Dom.MessageEvent
                let message = messageEvent.Data |> string
                JS.Console.Log("Received: " + message)
                
                match parseServerMessage message with
                | Some serverMsg ->
                    triggerEvent (MessageReceived serverMsg)
                | None ->
                    triggerEvent (Error "Invalid message format")
            
            // Add event listeners
            ws.AddEventListener("open", openHandler)
            ws.AddEventListener("close", closeHandler) 
            ws.AddEventListener("error", errorHandler)
            ws.AddEventListener("message", messageHandler)
            
            socket <- Some ws
            true
        with ex ->
            JS.Console.Error("Failed to connect: " + ex.Message)
            triggerEvent (Error ("Connection failed: " + ex.Message))
            false
    
    // Send a message to the server
    let sendMessage (message: ClientMessage) =
        match socket with
        | Some ws when ws.ReadyState = 1 -> // 1 = open
            try
                let json = messageToJson message
                let jsonStr = JSON.Stringify(json)
                JS.Console.Log("Sending: " + jsonStr)
                ws.Send(jsonStr)
                true
            with ex ->
                JS.Console.Error("Error sending message: " + ex.Message)
                false
        | _ ->
            JS.Console.Warn("Cannot send message: WebSocket not connected")
            false
    
    // Check if connected
    let isSocketConnected () = isConnected
    
    // Get WebSocket endpoint URL
    let getWebSocketUrl (baseUrl: string) =
        let wsProtocol = if baseUrl.StartsWith("https") then "wss" else "ws"
        let hostAndPort = baseUrl.Substring(baseUrl.IndexOf("://") + 3)
        $"{wsProtocol}://{hostAndPort}{GameConstants.WebSocketEndpoint}" 