namespace PixelTacToe.Server

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.FileProviders
open Microsoft.Extensions.Hosting
open PixelTacToe.Server.WebSockets

module Program =

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder args

        builder.Services.AddControllers()         |> ignore
        builder.Services.AddEndpointsApiExplorer()|> ignore
        builder.Services.AddSwaggerGen()          |> ignore

        builder.Services.AddCors(fun o ->
            o.AddPolicy("ClientCors", fun p ->
                p.WithOrigins("http://109.122.217.232:5000")
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowCredentials()
                 |> ignore)) |> ignore

        let app = builder.Build()

        if app.Environment.IsDevelopment() then
            app.UseSwagger()   |> ignore
            app.UseSwaggerUI() |> ignore

        let uploadsPath =
            Path.Combine(app.Environment.ContentRootPath,
                         "wwwroot", "uploads")

        let uploadOptions =
            new StaticFileOptions(
                FileProvider          = new PhysicalFileProvider(uploadsPath),
                RequestPath           = PathString("/uploads"),
                ServeUnknownFileTypes = true,
                OnPrepareResponse     = fun ctx ->
                    let hdr = ctx.Context.Response.Headers
                    hdr["Access-Control-Allow-Origin"]  <- "*"
                    hdr["Access-Control-Allow-Headers"] <- "Content-Type"
                    hdr["Access-Control-Allow-Methods"] <- "GET,OPTIONS" )

        app.UseStaticFiles(uploadOptions) |> ignore
        
        app.UseStaticFiles() |> ignore

        app.UseRouting()  |> ignore
        app.UseCors("ClientCors") |> ignore
        app.UseAuthorization() |> ignore

        app.UseWebSockets() |> ignore

        app.Use(fun (ctx: HttpContext) (next: Func<Task>) ->
            if  ctx.Request.Path =
                PathString PixelTacToe.Shared.Constants.GameConstants.WebSocketEndpoint
                && ctx.WebSockets.IsWebSocketRequest then
                task {
                    let! socket = ctx.WebSockets.AcceptWebSocketAsync()
                    do! WebSocketHandler.handleWebSocket ctx socket
                } :> Task
            else
                next.Invoke() ) |> ignore

        app.MapControllers() |> ignore

        app.Run()
        0
