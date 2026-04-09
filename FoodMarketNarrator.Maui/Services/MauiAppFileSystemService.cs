namespace food_market_narrator.Services;

public class MauiAppFileSystemService : IAppFileSystemService
{
    public string AppDataDirectory => FileSystem.AppDataDirectory;
}
