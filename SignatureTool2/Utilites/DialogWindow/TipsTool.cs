// SignTool.Utilites.DialogWindow.TipsTool
using System.Windows;

namespace SignatureTool2.Utilites.DialogWindow
{
    internal class TipsTool
    {
        public static MessageBoxResult OpenTipsWindow(string msg, string title, MessageBoxButton button, MessageBoxImage icon)
        {
            return MessageBox.Show(Application.Current.MainWindow, msg, title, button, icon);
        }
    } 
}
