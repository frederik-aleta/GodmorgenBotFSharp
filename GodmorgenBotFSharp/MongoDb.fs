module GodmorgenBotFSharp.MongoDb

open MongoDB.Driver

let mongoDatabaseName = "godmorgen"

let create (connectionString : string) =
    let mongoClient = new MongoClient (connectionString)
    let database = mongoClient.GetDatabase mongoDatabaseName
    database
