module GodmorgenBotFSharp.MessageHandler

open System
open System.Threading.Tasks
open MongoDB.Driver
open NetCord.Gateway
open FsToolkit.ErrorHandling

let messageCreate (ctx : Context) (message : Message) : ValueTask =
    task {
        if message.Author.IsBot then
            return ()
        else
            let isValidGodmorgenMessage = Validation.isValidGodmorgenMessage message.Content
            let isWeekend = Validation.isWeekend DateTime.UtcNow
            let isWithinGodmorgenHours = Validation.isWithinGodmorgenHours DateTime.UtcNow

            if isWeekend then
                ()
            else if isValidGodmorgenMessage && isWithinGodmorgenHours then
                let words = message.Content.Trim().ToLowerInvariant().Split ' '
                let gWord = words[0]
                let mWord = words[1]

                let userFilter = Builders<MongoDb.Types.GodmorgenStats>.Filter.Eq (_.DiscordUserId, message.Author.Id)
                let! godmorgenStatsO = ctx.MongoDataBase |> MongoDb.Functions.getGodmorgenStats userFilter

                match godmorgenStatsO |> Option.bind Array.tryHead with
                | None -> return ()
                | Some _ ->
                    let! _ = ctx.MongoDataBase |> MongoDb.Functions.giveUserPoint message.Author
                    let! _ = ctx.MongoDataBase |> MongoDb.Functions.updateWordCount message.Author gWord mWord

                    if message.Author.Id = Constants.ConlonDiscordUserId then
                        message.ReplyAsync $"Godmorgen <@{message.Author.Id}>, you little bitch! :blush:" |> ignore
                    else
                        message.ReplyAsync $"Godmorgen <@{message.Author.Id}>! :sun_with_face:" |> ignore
    }
    |> ValueTask
