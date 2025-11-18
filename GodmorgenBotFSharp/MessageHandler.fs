module GodmorgenBotFSharp.MessageHandler

open System.Threading.Tasks
open NetCord.Gateway

let messageCreate (ctx : Context) (message : Message) : ValueTask =
    task {
        if message.Author.IsBot then
            return ()
        else
            let! _ = message.ReplyAsync $"Hello, {message.Author.Username}!"
            return ()
    }
    |> ValueTask
