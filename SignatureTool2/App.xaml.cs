// SignTool.App
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using SignatureTool2;
using SignatureTool2.Utilites.Log;

namespace SignatureTool2
{
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LogTool.Instance.Init();
            MainWindowView mainWindowView = new MainWindowView();
            MainWindowViewModel mainWindowViewModel2 = (MainWindowViewModel)(mainWindowView.DataContext = new MainWindowViewModel());
            mainWindowView.ShowDialog();
        }
    } 
}
