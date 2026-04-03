using System.ComponentModel;

public interface IDataService : INotifyPropertyChanged
{
    /// <summary>
    /// 表示 SeedDataIfNeededAsync 是否正在執行（用於 UI 禁用按鈕）。
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// 表示是否需要顯示全域載入遮罩（涵蓋 SeedData + 首頁 DB 讀取兩個階段）。
    /// </summary>
    bool IsGlobalLoading { get; }

    /// <summary>
    /// 當載入狀態改變時觸發的事件。
    /// </summary>
    event Action OnStateChanged;

    /// <summary>
    /// 設定全域載入遮罩的顯示狀態。
    /// </summary>
    void SetGlobalLoading(bool value);

    /// <summary>
    /// 取得商品資料，並寫入資料庫
    /// </summary>
    Task SeedDataIfNeededAsync();
}