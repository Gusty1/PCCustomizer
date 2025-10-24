using System.ComponentModel;

public interface IDataService : INotifyPropertyChanged
{
    /// <summary>
    /// 表示目前是否正在載入資料。
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// 當載入狀態改變時觸發的事件。
    /// </summary>
    event Action OnStateChanged;

    /// <summary>
    /// 取得商品資料，並寫入資料庫
    /// </summary>
    /// <returns></returns>
    Task SeedDataIfNeededAsync();
}