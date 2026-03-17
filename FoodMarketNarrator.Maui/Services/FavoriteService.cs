using System.Text.Json;

namespace food_market_narrator.Services;

/// <summary>
/// Service quản lý danh sách yêu thích
/// Lưu vào Preferences (local storage) - không mất khi đóng app
/// </summary>
public interface IFavoriteService
{
    /// <summary>
    /// Lấy danh sách restaurantId yêu thích
    /// </summary>
    List<string> GetFavorites();

    /// <summary>
    /// Thêm vào yêu thích
    /// </summary>
    void AddFavorite(string restaurantId);

    /// <summary>
    /// Xóa khỏi yêu thích
    /// </summary>
    void RemoveFavorite(string restaurantId);

    /// <summary>
    /// Kiểm tra có yêu thích không
    /// </summary>
    bool IsFavorite(string restaurantId);
}

public class FavoriteService : IFavoriteService
{
    private const string FavoritesKey = "favorite_restaurants";
    private List<string> _favorites = new();

    public FavoriteService()
    {
        LoadFavorites();
    }

    private void LoadFavorites()
    {
        try
        {
            var json = Preferences.Get(FavoritesKey, null);
            if (!string.IsNullOrEmpty(json))
            {
                _favorites = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
        }
        catch
        {
            _favorites = new List<string>();
        }
    }

    private void SaveFavorites()
    {
        try
        {
            var json = JsonSerializer.Serialize(_favorites);
            Preferences.Set(FavoritesKey, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    public List<string> GetFavorites()
    {
        return new List<string>(_favorites);
    }

    public void AddFavorite(string restaurantId)
    {
        if (string.IsNullOrWhiteSpace(restaurantId))
            return;

        if (!_favorites.Contains(restaurantId))
        {
            _favorites.Add(restaurantId);
            SaveFavorites();
        }
    }

    public void RemoveFavorite(string restaurantId)
    {
        if (_favorites.Remove(restaurantId))
        {
            SaveFavorites();
        }
    }

    public bool IsFavorite(string restaurantId)
    {
        return _favorites.Contains(restaurantId);
    }
}
