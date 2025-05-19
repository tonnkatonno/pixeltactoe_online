namespace PixelTacToe.Client.Components

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html
open PixelTacToe.Shared.Models
open PixelTacToe.Client.Services

/// Component for the game board screen
[<JavaScript>]
module GameBoard =
    // Game state types from the service
    type private GameState = GameService.GameState
    type private GameData = GameService.GameData
    type private GameOutcome = GameService.GameOutcome
    
    // Handle cell click
    let private handleCellClick (row: int) (col: int) (e: Dom.MouseEvent) =
        GameService.makeMove row col |> ignore
    
    // Handle leave game button click
    let private handleLeaveGame (e: Dom.MouseEvent) =
        GameService.leaveGame() |> ignore
    
    // Render a cell in the game board
    let private renderCell (gameData: GameData) (row: int) (col: int) =
        let cellValue = gameData.CurrentBoard.[row, col]
        let isClickable = gameData.IsPlayerTurn && cellValue = ""
        
        let cellClass =
            if isClickable then "game-cell clickable" else "game-cell"
            
        let cellContent =
            match cellValue with
            | "X" -> Doc.Element "span" [attr.classDyn "player-mark"] [text "X"]
            | "O" -> Doc.Element "span" [attr.classDyn "opponent-mark"] [text "O"]
            | _ -> Doc.Empty

        div [
            attr.classDyn cellClass
            on.click (fun e -> 
                if isClickable then handleCellClick row col e
            )
        ] [cellContent]
    
    // Render the game board
    let private renderGameBoard (gameData: GameData) =
        let rows = [0..2]
        let cols = [0..2]
        
        div [attr.classDyn "game-board"] [
            for row in rows do
                div [attr.classDyn "board-row"] [
                    for col in cols do
                        renderCell gameData row col
                ]
        ]
    
    // Render player info (avatar and name)
    let private renderPlayerInfo (player: Player) (label: string) (isTurn: bool) =
        div [
            attr.classDyn (if isTurn then "player-info active" else "player-info")
        ] [
            div [attr.classDyn "player-avatar"] [
                img [
                    attr.src player.ImageUrl
                    attr.alt (player.Name + "'s avatar")
                ]
            ]
            div [attr.classDyn "player-name"] [
                span [] [text label]
                h3 [] [text player.Name]
            ]
        ]
    
    // Render game status message
    let private renderGameStatus (gameData: GameData) =
        let statusMessage =
            if gameData.IsPlayerTurn then
                "Your turn"
            else
                "Opponent's turn"
                
        div [attr.classDyn "game-status"] [
            h2 [] [text statusMessage]
        ]
    
    // Render game over message
    let private renderGameOver (gameData: GameData) (outcome: GameOutcome) =
        let (message, className) =
            match outcome with
            | GameService.Win -> "You won!", "game-over win"
            | GameService.Loss -> "You lost!", "game-over loss"
            | GameService.Draw -> "It's a draw!", "game-over draw"
            | GameService.Abandoned -> "Opponent left the game", "game-over abandoned"
            
        div [attr.classDyn className] [
            h2 [] [text message]
            button [
                attr.classDyn "btn btn-primary"
                on.click handleLeaveGame
            ] [text "Back to Lobby"]
        ]
    
    // Main render function
    let render () =
        let gameStateView = Var.View GameService.gameStateVar
        
        Doc.BindView (fun gameState ->
            match gameState with
            | GameState.InGame gameData ->
                div [attr.classDyn "game-container"] [
                    div [attr.classDyn "game-header"] [
                        renderPlayerInfo gameData.Player "You" gameData.IsPlayerTurn
                        renderGameStatus gameData
                        renderPlayerInfo gameData.Opponent "Opponent" (not gameData.IsPlayerTurn)
                    ]
                    renderGameBoard gameData
                    div [attr.classDyn "game-controls"] [
                        button [
                            attr.classDyn "btn btn-secondary"
                            on.click handleLeaveGame
                        ] [text "Forfeit Game"]
                    ]
                ]
                
            | GameState.GameOver (gameData, outcome) ->
                div [attr.classDyn "game-container"] [
                    div [attr.classDyn "game-header"] [
                        renderPlayerInfo gameData.Player "You" false
                        renderGameOver gameData outcome
                        renderPlayerInfo gameData.Opponent "Opponent" false
                    ]
                    renderGameBoard gameData
                ]
                
            | _ ->
                // Should not be rendered in other states
                Doc.Empty
        ) gameStateView 