using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SignatureTool2.Utilites.Extensions;
using SignatureTool2.ViewModel;

namespace SignatureTool2.View
{
    public partial class SyncCleanDialog : Window
    {
        public SyncCleanDialog(List<SyncCleanItem> items)
        {
            InitializeComponent();
            DataContext = new SyncCleanContext { Items = items };
        }

        private void OnKeepAll(object sender, RoutedEventArgs e)
        {
            SetAll(true);
        }

        private void OnDeleteAll(object sender, RoutedEventArgs e)
        {
            SetAll(false);
        }

        private void OnConfirm(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void SetAll(bool keep)
        {
            if (DataContext is SyncCleanContext context && context.Items != null)
            {
                foreach (SyncCleanItem item in context.Items)
                {
                    item.IsKeep = keep;
                }
            }
        }

        /// <summary>弹出清理确认框；返回用户确认要保留的文件列表（取消返回 null）。</summary>
        public static List<string> Show(List<string> staleFiles)
        {
            List<SyncCleanItem> items = staleFiles.Select(p => new SyncCleanItem
            {
                FilePath = p,
                FileName = p.GetFileName(),
            }).ToList();
            SyncCleanDialog dialog = new SyncCleanDialog(items);
            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                return null;
            }
            return items.Where(p => p.IsKeep).Select(p => p.FilePath).ToList();
        }
    }
}
