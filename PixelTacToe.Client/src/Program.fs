namespace PixelTacToe.Client

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html
open PixelTacToe.Client.Components
open PixelTacToe.Client.Services

module Program =
    [<SPAEntryPoint>]
    let Main() =
        // Setup services
        WebSocketService.initialize()
        GameService.initialize()
        
        // Render the main application component
        let appElement = 
            App.render()
            
        // Mount the application to the DOM
        let appContainer = JS.Document.GetElementById("app-container")
        Doc.RunById "app-container" appElement 

        App.start() 