using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiAOIViewer.Shared.Models
{
    public class ImageItem
    {
        public string FileName { get; set; } = string.Empty;
        public string Base64 { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }

        public int FlawId { get; set; }               // 檢驗編號
        public string DefectName { get; set; } = "";  // 瑕疵名稱
        public DateTime? DefectTime { get; set; }     // 發生時間
    }
}
