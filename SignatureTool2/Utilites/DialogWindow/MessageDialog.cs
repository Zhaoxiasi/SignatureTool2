using System;
using System.Windows;
using System.Windows.Controls;

namespace SignatureTool2.Utilites.DialogWindow
{
    public class MessageDialog
    {
        public static MessageBoxResult Show(string message, string title, MessageBoxButton button = MessageBoxButton.OK)
        {
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                return (MessageBoxResult)Application.Current.Dispatcher.Invoke(new Func<MessageBoxResult>(() => Show(message, title, button)));
            }

            Window window = new Window
            {
                Title = title,
                Width = 650,
                Height = 480,
                MinWidth = 450,
                MinHeight = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanResizeWithGrip
            };

            if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                window.Owner = Application.Current.MainWindow;
            }

            Grid rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 内容区
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 按钮区

            // 可滚动的文本区域，支持全选复制
            TextBox textBox = new TextBox
            {
                Text = message,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(12),
                FontFamily = new System.Windows.Media.FontFamily("Consolas, 微软雅黑"),
                FontSize = 13
            };
            Grid.SetRow(textBox, 0);
            rootGrid.Children.Add(textBox);

            // 底部按钮区域
            StackPanel btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(12, 0, 12, 12)
            };
            Grid.SetRow(btnPanel, 1);

            MessageBoxResult result = MessageBoxResult.Cancel;

            if (button == MessageBoxButton.YesNo || button == MessageBoxButton.YesNoCancel)
            {
                Button btnYes = new Button { Content = "是 (Yes)", Width = 90, Height = 28, Margin = new Thickness(5, 0, 5, 0), IsDefault = true };
                btnYes.Click += (s, e) => { result = MessageBoxResult.Yes; window.Close(); };
                btnPanel.Children.Add(btnYes);

                Button btnNo = new Button { Content = "否 (No)", Width = 90, Height = 28, Margin = new Thickness(5, 0, 5, 0) };
                btnNo.Click += (s, e) => { result = MessageBoxResult.No; window.Close(); };
                btnPanel.Children.Add(btnNo);
            }

            if (button == MessageBoxButton.OK || button == MessageBoxButton.OKCancel)
            {
                Button btnOk = new Button { Content = "确定 (OK)", Width = 90, Height = 28, Margin = new Thickness(5, 0, 5, 0), IsDefault = true };
                btnOk.Click += (s, e) => { result = MessageBoxResult.OK; window.Close(); };
                btnPanel.Children.Add(btnOk);
            }

            if (button == MessageBoxButton.YesNoCancel || button == MessageBoxButton.OKCancel)
            {
                Button btnCancel = new Button { Content = "取消", Width = 90, Height = 28, Margin = new Thickness(5, 0, 5, 0), IsCancel = true };
                btnCancel.Click += (s, e) => { result = MessageBoxResult.Cancel; window.Close(); };
                btnPanel.Children.Add(btnCancel);
            }

            rootGrid.Children.Add(btnPanel);
            window.Content = rootGrid;

            window.ShowDialog();
            return result;
        }
    }
}

