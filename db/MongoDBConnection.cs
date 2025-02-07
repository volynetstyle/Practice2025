using MongoDB.Driver;
using MongoDB.Bson;
using System;
using DotNetEnv;

namespace practice.db
{
    class MongoDBConnection
    {
        private static readonly MongoClient _client;
        private static readonly IMongoDatabase _database;

        static MongoDBConnection()
        {

            var connectionUri = Environment.GetEnvironmentVariable("DATABASE_URL");

            if (string.IsNullOrEmpty(connectionUri))
            {
                throw new ArgumentNullException("DATABASE_URL", "MongoDB connection string is not provided in environment variables.");
            }

            var settings = MongoClientSettings.FromConnectionString(connectionUri);

            settings.ServerApi = new ServerApi(ServerApiVersion.V1);

            _client = new MongoClient(settings);
            _database = _client.GetDatabase("Dictionary");

            try
            {
                var result = _client.GetDatabase("Dictionary").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
                Console.WriteLine("Pinged your deployment. You successfully connected to MongoDB!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static IMongoDatabase Database => _database;
    }
}
