module GodmorgenBotFSharp.MongoDb.Functions

open System
open System.Linq.Expressions
open System.Threading.Tasks
open GodmorgenBotFSharp.MongoDb.Types
open MongoDB.Driver
open FsToolkit.ErrorHandling

[<Literal>]
let mongoDatabaseName : string = "godmorgen"

[<Literal>]
let godmorgenStatsCollectionName : string = "godmorgen_stats"

let create (connectionString : string) : IMongoDatabase =
    let mongoClient = new MongoClient (connectionString)
    let database = mongoClient.GetDatabase mongoDatabaseName
    database

let getGodmorgenStats (filter : FilterDefinition<GodmorgenStats>) (mongoDatabase : IMongoDatabase) =
    task {
        let collection = mongoDatabase.GetCollection<GodmorgenStats> godmorgenStatsCollectionName
        let! results = collection.Find(filter).ToListAsync ()
        return results |> Seq.toArray
    }

let giveUserPoint (user : NetCord.User) (mongoDatabase : IMongoDatabase) =
    task {
        let collection = mongoDatabase.GetCollection<GodmorgenStats> godmorgenStatsCollectionName
        let mongoId = GodmorgenStats.createMongoId user.Id DateTime.Today
        let! mongoUser = collection.Find(fun x -> x.Id = mongoId).FirstOrDefaultAsync ()

        if box mongoUser = null then
            let newUser = GodmorgenStats.create user.Id user.Username
            do! collection.InsertOneAsync newUser

            return {|
                Previous = 0
                Current = 1
            |}
        else
            let updatedUser = GodmorgenStats.increaseGodmorgenCount mongoUser
            let! _ = collection.ReplaceOneAsync ((fun x -> x.Id = mongoId), updatedUser)

            return {|
                Previous = mongoUser.GodmorgenCount
                Current = updatedUser.GodmorgenCount
            |}
    }

let updateWordCount (user : NetCord.User) (gWord : string) (mWord : string) (mongoDatabase : IMongoDatabase) =
    task {
        let collection = mongoDatabase.GetCollection<WordCount> $"word_count_{user.Id}"

        let upsertWord word =
            task {
                let filter = Builders<WordCount>.Filter.Eq ((fun x -> x.Word), word)
                let update = Builders<WordCount>.Update.Inc ((fun x -> x.Count), 1)
                let options = FindOneAndUpdateOptions<WordCount> ()
                options.IsUpsert <- true
                do! collection.FindOneAndUpdateAsync (filter, update, options) :> Task
            }

        let! _ = Task.WhenAll ([| upsertWord gWord ; upsertWord mWord |])
        return ()
    }

let getHereticUserIds (mongoDatabase : IMongoDatabase) =
    task {
        let collection = mongoDatabase.GetCollection<GodmorgenStats> godmorgenStatsCollectionName
        let today = DateTime.UtcNow.Date
        let currentMonth = today.Month
        let currentYear = today.Year

        let! messages = collection.Find(fun msg -> msg.Month = currentMonth && msg.Year = currentYear).ToListAsync ()

        return
            messages
            |> Seq.filter (fun x -> x.LastGoodmorgenDate < DateTimeOffset today)
            |> Seq.map _.DiscordUserId
            |> Seq.distinct
            |> Array.ofSeq
    }

let getWordCount (user : NetCord.User) (gWord : string) (mWord : string) (mongoDatabase : IMongoDatabase) =
    task {
        let collection = mongoDatabase.GetCollection<WordCount> $"word_count_{user.Id}"
        let filter = Builders<WordCount>.Filter.In ((fun x -> x.Word), [| gWord ; mWord |])
        let! results = collection.Find(filter).ToListAsync ()
        let counts = results |> Seq.map (fun x -> x.Word, x.Count) |> Map.ofSeq

        return {|
            gWordCount = counts[gWord]
            mWordCount = counts[mWord]
        |}
    }

let getTop5Words (user : NetCord.User) (mongoDatabase : IMongoDatabase) =
    task {
        let collection = mongoDatabase.GetCollection<WordCount> $"word_count_{user.Id}"
        let! results = collection.Find(fun _ -> true).SortByDescending(fun x -> x.Count).Limit(5).ToListAsync ()
        return results |> Seq.toArray
    }
