using MauiAOIViewer.Services;
using MauiAOIViewer.Shared.Models;
using Microsoft.Data.SqlClient;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;

namespace MauiAOIViewer.Shared.Services
{
    public class ImageDecodeService
    {
        private readonly IAppPathService _appPathService;
        private const string ConnStr = "Server=127.0.0.1;Database=AI;User Id=sa;Password=1qaz2wsx~;TrustServerCertificate=True";

        public ImageDecodeService(IAppPathService appPathService)
        {
            _appPathService = appPathService;
        }

        // 儲存圖檔
        private void SaveImageFile(string fileName, byte[] data)
        {
            string imageDir = _appPathService.GetImageSaveDirectory();
            string fullPath = Path.Combine(imageDir, fileName + ".jpg");
            File.WriteAllBytes(fullPath, data);
        }

        #region JobID 與 Defect 統計

        public async Task<List<string>> GetRecentJobIdsAsync(int topCount = 25)
        {
            var result = new List<string>();
            string sql = $"SELECT DISTINCT TOP (@TopCount) JobID FROM [AI].[dbo].[WINTRISS_PM1] ORDER BY JobID DESC";

            using SqlConnection conn = new(ConnStr);
            await conn.OpenAsync();
            using SqlCommand cmd = new(sql, conn);
            cmd.Parameters.AddWithValue("@TopCount", topCount);

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string? jobId = reader["JobID"]?.ToString();
                if (!string.IsNullOrWhiteSpace(jobId))
                    result.Add(jobId);
            }
            return result;
        }

        public async Task<List<DefectSummary>> GetTop10DefectsAsync(string jobId)
        {
            string sql = @"
                SELECT TOP 10 DefectName, COUNT(*) AS Count
                FROM dbo.WINTRISS_PM1
                WHERE JobID = @JobID
                GROUP BY DefectName
                ORDER BY COUNT(*) DESC";

            var result = new List<DefectSummary>();
            using SqlConnection conn = new(ConnStr);
            using SqlCommand cmd = new(sql, conn);
            cmd.Parameters.AddWithValue("@JobID", jobId);

            await conn.OpenAsync();
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new DefectSummary
                {
                    DefectName = reader["DefectName"]?.ToString() ?? "",
                    Count = Convert.ToInt32(reader["Count"])
                });
            }
            return result;
        }

        #endregion

        #region 圖片查詢與分頁

        public async Task<List<ImageItem>> GetImagesByJobIdAsync(string jobId, int pageIndex, int pageSize)
        {
            string sql = @"
                SELECT TOP (@PageSize) FileName, iImage, lFlawId, DefectName, DefectTime
                FROM (
                    SELECT FileName, iImage, lFlawId, DefectName, DefectTime, ROW_NUMBER() OVER (ORDER BY lFlawId ASC) AS RowNum
                    FROM dbo.WINTRISS_PM1 WHERE JobID = @JobID
                ) AS T
                WHERE RowNum > (@PageSize * (@PageIndex - 1))
                ORDER BY RowNum";

            return await ReadImageDataAsync(sql, new()
            {
                { "@JobID", jobId },
                { "@PageSize", pageSize },
                { "@PageIndex", pageIndex }
            });
        }

        public async Task<List<ImageItem>> GetImagesByJobIDAndDefectNameAsync(string jobId, string defectName, int pageIndex, int pageSize)
        {
            string sql = @"
                SELECT TOP (@PageSize) FileName, iImage, lFlawId, DefectName, DefectTime
                FROM (
                    SELECT FileName, iImage, lFlawId, DefectName, DefectTime, ROW_NUMBER() OVER (ORDER BY lFlawId ASC) AS RowNum
                    FROM dbo.WINTRISS_PM1
                    WHERE JobID = @JobID AND DefectName LIKE @DefectName
                ) AS T
                WHERE RowNum > (@PageSize * (@PageIndex - 1))
                ORDER BY RowNum";

            return await ReadImageDataAsync(sql, new()
            {
                { "@JobID", jobId },
                { "@DefectName", $"{defectName}%" },
                { "@PageSize", pageSize },
                { "@PageIndex", pageIndex }
            });
        }

        private async Task<List<ImageItem>> ReadImageDataAsync(string sql, Dictionary<string, object> parameters)
        {
            var result = new List<ImageItem>();

            using SqlConnection conn = new(ConnStr);
            await conn.OpenAsync();
            using SqlCommand cmd = new(sql, conn);

            foreach (var param in parameters)
                cmd.Parameters.AddWithValue(param.Key, param.Value);

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                try
                {
                    string? fileNameRaw = reader["FileName"]?.ToString();
                    if (string.IsNullOrEmpty(fileNameRaw))
                        continue;

                    string fileName = Path.GetFileNameWithoutExtension(fileNameRaw);
                    byte[] imageBytes = (byte[])reader["iImage"];
                    if (imageBytes.Length < 6) continue;

                    using var image = DecodeToImageSharp(imageBytes);
                    using var ms = new MemoryStream();
                    image.Save(ms, new JpegEncoder());
                    string base64 = Convert.ToBase64String(ms.ToArray());

                    string imageDir = _appPathService.GetImageSaveDirectory();
                    string savePath = Path.Combine(imageDir, fileName + ".jpg");
                    File.WriteAllBytes(savePath, ms.ToArray());

                    result.Add(new ImageItem
                    {
                        FileName = fileName,
                        Base64 = base64,
                        DownloadUrl = savePath,
                        Width = image.Width,
                        Height = image.Height,
                        FlawId = Convert.ToInt32(reader["lFlawId"]),
                        DefectName = reader["DefectName"]?.ToString() ?? "",
                        DefectTime = reader["DefectTime"] as DateTime?
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Decode Image Error] {ex.Message}");
                }
            }
            return result;
        }

        #endregion

        #region 總筆數查詢

        public async Task<int> GetTotalCountByJobIdAsync(string jobId)
        {
            string sql = "SELECT COUNT(*) FROM dbo.WINTRISS_PM1 WHERE JobID = @JobID";

            using SqlConnection conn = new(ConnStr);
            await conn.OpenAsync();
            using SqlCommand cmd = new(sql, conn);
            cmd.Parameters.AddWithValue("@JobID", jobId);

            return (int)await cmd.ExecuteScalarAsync();
        }

        public async Task<int> GetTotalCountByJobIdAndDefectNameAsync(string jobId, string defectName)
        {
            string sql = "SELECT COUNT(*) FROM dbo.WINTRISS_PM1 WHERE JobID = @JobID AND DefectName LIKE @DefectName";

            using SqlConnection conn = new(ConnStr);
            await conn.OpenAsync();
            using SqlCommand cmd = new(sql, conn);
            cmd.Parameters.AddWithValue("@JobID", jobId);
            cmd.Parameters.AddWithValue("@DefectName", $"{defectName}%");

            return (int)await cmd.ExecuteScalarAsync();
        }

        #endregion

        #region 解碼函數

        private static Image<Rgba32> DecodeToImageSharp(byte[] a)
        {
            int width = a[0] + a[1] * 256;
            int height = a[4] + a[5] * 256;
            if (width <= 0 || height <= 0)
                throw new ArgumentException("無效圖檔尺寸");

            var image = new Image<Rgba32>(width, height);
            int index = 7;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (index >= a.Length) break;
                    byte gray = a[index++];
                    image[x, y] = new Rgba32(gray, gray, gray);
                }
            }

            return image;
        }

        #endregion
    }
}
