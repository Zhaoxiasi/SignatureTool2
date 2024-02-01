// SignTool.MainWindowView
using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using SignatureTool2;
using SignatureTool2.Utilites.Sign;

namespace SignatureTool2
{
    public partial class MainWindowView : Window, IComponentConnector
    {

        public MainWindowView()
        {
            InitializeComponent();
            var cor = SystemParameters.WindowCornerRadius;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SignatureTool.IsAllSkip = true;
        }

        private void SettingBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    } 
}
