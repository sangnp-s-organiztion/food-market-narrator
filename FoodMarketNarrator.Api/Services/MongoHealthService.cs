using food_market_narrator_api.DTOs.Mongo;
using food_market_narrator_api.Models;
using food_market_narrator_api.Repositories;
using Microsoft.Extensions.Options;

namespace food_market_narrator_api.Services;

public class MongoHealthService
{
    private readonly MongoHealthRepository _mongoHealthRepository;
    private readonly MongoDbSettings _mongoDbSettings;

    public MongoHealthService(
        MongoHealthRepository mongoHealthRepository,
        IOptions<MongoDbSettings> mongoOptions)
    {
        _mongoHealthRepository = mongoHealthRepository;
        _mongoDbSettings = mongoOptions.Value;
    }

    public async Task<MongoConnectionTestResponse> TestConnectionAsync()
    {
        var (isConnected, message) = await _mongoHealthRepository.TestConnectionAsync();

        return new MongoConnectionTestResponse
        {
            Success = isConnected,
            Message = message,
            DatabaseName = _mongoDbSettings.DatabaseName,
            CheckedAtUtc = DateTime.UtcNow
        };
    }
}
