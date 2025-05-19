namespace PixelTacToe.Client.Services

open System
open WebSharper
open WebSharper.JavaScript
open WebSharper.Web
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html

/// Service for uploading and managing player avatar images
[<JavaScript>]
module ImageUploadService =
    // Default image URL - using a placeholder image URL
    let defaultImageUrl = "/img/default-player.png"
    
    // Mutable reference to store the current image URL
    let mutable currentImageUrl = defaultImageUrl
    
    // Get the current image URL (either uploaded or default)
    let getCurrentImageUrl() =
        // Check if there's a global variable set by the upload process
        let uploadedImage = JS.Window?WebSharper?SkinUploadData
        
        if not (isNull uploadedImage) then
            string uploadedImage
        else
            currentImageUrl
    
    // Get the default image URL
    let getDefaultImageUrl() = defaultImageUrl
    
    // Handle file upload from input element
    let uploadImageFromInput (inputElement: Dom.Element) =
        try
            // Cast to HTMLInputElement
            let fileInput = inputElement :?> Dom.HTMLInputElement
            
            // Check if files were selected
            if fileInput.Files.Length > 0 then
                // Get the selected file
                let file = fileInput.Files.[0]
                
                // Create a FileReader to read the image
                let reader = New<Dom.FileReader>()
                
                // Set up load handler
                let loadHandler (e: Dom.Event) =
                    try
                        // Get the data URL from the result
                        let dataUrl = reader.Result |> string
                        
                        // Store the result in a global variable for access from server
                        JS.Window?WebSharper?SkinUploadData <- dataUrl
                        
                        // Update the current URL
                        currentImageUrl <- dataUrl
                    with ex ->
                        JavaScript.Console.Error("Error processing uploaded image: " + ex.Message)
                
                // Set up error handler
                let errorHandler (e: Dom.Event) =
                    JavaScript.Console.Error("Error reading file")
                
                // Add event listeners
                reader.AddEventListener("load", loadHandler)
                reader.AddEventListener("error", errorHandler)
                
                // Read the file as a data URL
                reader.ReadAsDataURL(file)
                true
            else
                false
        with ex ->
            JavaScript.Console.Error("Error during file upload: " + ex.Message)
            false
    
    // Function to trigger file input click
    let triggerFileInput (fileInput: Dom.Element) =
        try
            let input = fileInput :?> Dom.HTMLInputElement
            input.Click()
            true
        with ex ->
            JavaScript.Console.Error("Error triggering file input: " + ex.Message)
            false 