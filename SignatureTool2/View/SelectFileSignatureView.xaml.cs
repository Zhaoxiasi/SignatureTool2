// SignTool.View.SelectFileSignatureView
using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using SignatureTool2.ViewModel;

namespace SignatureTool2.View
{
    public partial class SelectFileSignatureView : UserControl, IComponentConnector
    {

        public SelectFileSignatureView()
        {
            InitializeComponent();
        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] array = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (array != null && array.Length != 0)
                {
                    (base.DataContext as SelectFileSignatureViewModel).FileDroped(array);
                }
            }
        }

        private void ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                (base.DataContext as SelectFileSignatureViewModel).OnRemoveItemCommand("signle");
            }
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.V && (e.KeyboardDevice.Modifiers & ModifierKeys.Control )== ModifierKeys.Control)
            {
                var sl = Clipboard.GetFileDropList();
                if (sl != null && sl.Count != 0)
                {
                    string[] array = new string[sl.Count];
                    sl.CopyTo(array, 0);
                    (base.DataContext as SelectFileSignatureViewModel).FileDroped(array);
                }
            }
        }
    } 
}
