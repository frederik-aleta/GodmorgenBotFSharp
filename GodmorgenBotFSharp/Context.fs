namespace GodmorgenBotFSharp

open Microsoft.Extensions.Logging
open MongoDB.Driver

type Context = {
    MongoDataBase: IMongoDatabase
    Logger: ILogger
}
