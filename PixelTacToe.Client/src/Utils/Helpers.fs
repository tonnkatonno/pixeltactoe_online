namespace PixelTacToe.Client.Utils

open System
open WebSharper
open WebSharper.JavaScript

/// Helper utility functions for the client
[<JavaScript>]
module Helpers =
    // Get the base URL for the application
    let getServerBaseUrl() =
        let protocol = JS.Window.Location.Protocol
        let hostname = JS.Window.Location.Hostname
        let port = JS.Window.Location.Port
        $"{protocol}//{hostname}:{port}"
        
    // Safe window.setTimeout wrapper
    let setTimeout (callback: unit -> unit) (ms: int) =
        JS.Window.SetTimeout(callback, ms) |> ignore
        
    // Safe console logging
    let log (message: string) =
        Console.Log(message)
        
    let warn (message: string) =
        Console.Warn(message)
        
    let error (message: string) =
        Console.Error(message)
        
    // Safe DOM element access
    let getElementById (id: string) =
        let element = JS.Document.GetElementById(id)
        if isNull element then None else Some element
        
    // Parse query parameters from URL
    let getQueryParam (name: string) =
        let url = JS.Window.Location.Search.Substring(1)
        let vars = url.Split('&')
        
        vars
        |> Array.tryFind (fun item -> 
            let parts = item.Split('=')
            parts.Length > 0 && parts.[0] = name)
        |> Option.map (fun item ->
            let parts = item.Split('=')
            if parts.Length > 1 then decodeURIComponent parts.[1] else "")
            
    // Store value in localStorage
    let storeValue (key: string) (value: string) =
        try
            JS.Window.LocalStorage.SetItem(key, value)
            true
        with _ ->
            false
            
    // Get value from localStorage
    let getValue (key: string) =
        try
            let value = JS.Window.LocalStorage.GetItem(key)
            if isNull value then None else Some (string value)
        with _ ->
            None
            
    [<Direct("return decodeURIComponent($0);")>]
    let decodeURIComponent (s: string) : string = 
        failwith "Never called" 