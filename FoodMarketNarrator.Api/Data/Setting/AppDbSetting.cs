public class AppSetting
{
    public string ConnectionString { get; set; }
    public AppSetting(string connectionString)
    {
        ConnectionString = connectionString;
    }
}