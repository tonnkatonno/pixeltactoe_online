namespace PixelTacToe.Server

open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.FileProviders
open Microsoft.Extensions.Hosting

module Program =

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder args
  

        builder.Services.AddControllers()         |> ignore
        builder.Services.AddEndpointsApiExplorer()|> ignore
        builder.Services.AddSwaggerGen()          |> ignore

        builder.Services.AddCors(fun o ->
            o.AddPolicy("ClientCors", fun p ->
                p.WithOrigins("http://localhost:5000")
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowCredentials()
                 |> ignore)) |> ignore

        let app = builder.Build()
        
        let clearAndEnsure dirs =
            for relativePath in dirs do
                let fullPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", relativePath)
                if Directory.Exists fullPath then
                    Directory.Delete(fullPath, true)
                Directory.CreateDirectory(fullPath) |> ignore

        clearAndEnsure [ "game"; "uploads" ]

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

        app.MapControllers() |> ignore

        app.Run()
        0
