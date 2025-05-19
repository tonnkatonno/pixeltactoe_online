module PixelTacToe.Client.Program

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder args

    builder.Services.AddRouting() |> ignore
    let app = builder.Build()

    if app.Environment.IsDevelopment() then
        app.UseDeveloperExceptionPage() |> ignore

    app.UseStaticFiles()   |> ignore

    let routeHandlerBuilder =
      app.MapGet("/", Func<HttpContext,Task>(fun ctx ->
          let html = """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width,initial-scale=1">
  <title>PixelTacToe</title>
  <link rel="stylesheet" href="/css/site.css">
</head>
<body>
  <div class="wrapper">
    <h1>PixelTacToe</h1>

    <div class="section">
      <h2>Current image</h2>
      <img id="preview" class="preview" src="/img/default-player.png" alt="preview">
    </div>

    <div class="section">
      <h2>Upload new image</h2>
      <label for="nameInput">Player name</label>
      <input id="nameInput" type="text" placeholder="Alice" required>
      <label for="fileInput">Choose an image</label>
      <input id="fileInput" type="file" accept="image/*">
    </div>

    <div class="section action-row">
      <button id="defaultBtn"  class="btn btn-secondary">Use default image</button>
      <button id="createBtn"   class="btn btn-primary">Create Player</button>
      <span   id="status"></span>
    </div>
      <div class="section">
          <div class="section">
        <h2>Incoming match requests</h2>
        <ul id="requestList" class="lobby-list"></ul>
         </div>
        <h2>Players in lobby</h2>
        <ul id="lobbyList" class="lobby-list"></ul>
        <h3>Leaderboard</h3>
        <ul id="leaders" class="leaderboard"></ul>
      </div>

  </div>

  <script src="/js/app.js"></script>
</body>
</html>"""
          ctx.Response.ContentType <- "text/html"
          ctx.Response.WriteAsync html ))

    app.Run()
    0
