open System.Threading.Tasks
open GodmorgenBotFSharp
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open MongoDB.Driver
open NetCord
open NetCord.Gateway
open NetCord.Hosting.Gateway
open NetCord.Hosting.Services.ApplicationCommands

let builder =
    Host
        .CreateDefaultBuilder()
        .ConfigureAppConfiguration(fun context config -> //
            config.AddJsonFile ("local.settings.json", optional = false, reloadOnChange = true)
            |> ignore
        )
        .ConfigureServices (fun hostBuilderContext serviceCollection ->
            serviceCollection.AddLogging () |> ignore
            serviceCollection.AddHostedService<BackgroundTaskTest.BackgroundWorker> () |> ignore
            serviceCollection.AddDiscordGateway () |> ignore
            serviceCollection.AddApplicationCommands () |> ignore
        )

let host = builder.Build ()
let gatewayClient = host.Services.GetRequiredService<GatewayClient> ()
let loggerFactory = host.Services.GetRequiredService<ILoggerFactory> ()
let configuration = host.Services.GetRequiredService<IConfiguration> ()
let mongoConnectionString = configuration.GetConnectionString "MongoDb"

let ctx = {
    MongoDataBase = MongoDb.create mongoConnectionString
    Logger = loggerFactory.CreateLogger "GodmorgenBot"
}

gatewayClient.add_MessageCreate (MessageHandler.messageCreate ctx)

type Delegate = delegate of User -> string
let delegateFunc = Delegate (fun user -> $"Pong! <@{user.Id}>")
host.AddSlashCommand ("ping", "Replies with Pong!", SlashCommands.pingCommand) |> ignore
host.AddSlashCommand ("ping", "Replies with Pong!", SlashCommands.leaderboardCommand) |> ignore

host.RunAsync () |> Async.AwaitTask |> Async.RunSynchronously
