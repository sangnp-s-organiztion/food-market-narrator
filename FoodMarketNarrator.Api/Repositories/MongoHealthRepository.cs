using MongoDB.Bson;
using MongoDB.Driver;

namespace food_market_narrator_api.Repositories;

public class MongoHealthRepository
{
    private readonly IMongoDatabase _mongoDatabase;

    public MongoHealthRepository(IMongoDatabase mongoDatabase)
    {
        _mongoDatabase = mongoDatabase;
    }

    public async Task<(bool IsConnected, string Message)> TestConnectionAsync()
    {
        try
        {
            var pingCommand = new BsonDocument("ping", 1);
            await _mongoDatabase.RunCommandAsync<BsonDocument>(pingCommand);
            return (true, "MongoDB connection successful.");
        }
        catch (Exception ex)
        {
            return (false, $"MongoDB connection failed: {ex.Message}");
        }
    }
}
