namespace MauiAOIViewer.Services
{
    /// <summary>
    /// 提供平台相依的路徑資訊，例如圖片儲存路徑。
    /// 每個平台需實作對應的服務。
    /// </summary>
    public interface IAppPathService
    {
        /// <summary>
        /// 取得圖片儲存目錄的完整路徑。
        /// </summary>
        /// <returns>平台專屬的目錄路徑</returns>
        string GetImageSaveDirectory();
    }
}
