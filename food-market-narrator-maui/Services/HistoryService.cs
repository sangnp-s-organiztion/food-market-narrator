namespace food_market_narrator.Services;

/// <summary>
/// Service quản lý lịch sử xem quán
/// Lưu vào memory - reset khi đóng app
/// </summary>
public interface IHistoryService
{
    /// <summary>
    /// Lấy danh sách restaurantId đã xem (mới nhất trước)
    /// </summary>
    List<string> GetHistory();

    /// <summary>
    /// Thêm vào lịch sử (nếu đã có thì chuyển lên đầu)
    /// </summary>
    void AddToHistory(string restaurantId);

    /// <summary>
    /// Xóa một mục khỏi lịch sử
    /// </summary>
    void RemoveFromHistory(string restaurantId);

    /// <summary>
    /// Xóa toàn bộ lịch sử
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// Kiểm tra có trong lịch sử không
    /// </summary>
    bool IsInHistory(string restaurantId);
}

public class HistoryService : IHistoryService
{
    // Lưu trong memory - sẽ reset khi đóng app
    private readonly List<string> _history = new();

    // Số lượng tối đa lưu trong lịch sử
    private const int MaxHistoryItems = 50;

    public List<string> GetHistory()
    {
        return new List<string>(_history);
    }

    public void AddToHistory(string restaurantId)
    {
        if (string.IsNullOrWhiteSpace(restaurantId))
            return;

        // Nếu đã có thì xóa vị trí cũ và thêm vào đầu
        _history.Remove(restaurantId);
        _history.Insert(0, restaurantId);

        // Giới hạn số lượng
        while (_history.Count > MaxHistoryItems)
        {
            _history.RemoveAt(_history.Count - 1);
        }
    }

    public void RemoveFromHistory(string restaurantId)
    {
        _history.Remove(restaurantId);
    }

    public void ClearHistory()
    {
        _history.Clear();
    }

    public bool IsInHistory(string restaurantId)
    {
        return _history.Contains(restaurantId);
    }
}
