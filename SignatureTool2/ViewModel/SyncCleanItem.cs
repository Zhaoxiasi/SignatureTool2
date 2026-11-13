using System.Collections.Generic;

namespace SignatureTool2.ViewModel
{
    /// <summary>严格同步清理确认对话框中的单个文件条目。</summary>
    public class SyncCleanItem : ViewModelBase
    {
        private bool isKeep;

        public string FilePath { get; set; }

        public string FileName { get; set; }

        /// <summary>勾选后该文件将被保留，不参与清理。</summary>
        public bool IsKeep
        {
            get
            {
                return isKeep;
            }
            set
            {
                SetProperty(ref isKeep, value, "IsKeep");
            }
        }
    }

    public class SyncCleanContext
    {
        public List<SyncCleanItem> Items { get; set; }
    }
}
