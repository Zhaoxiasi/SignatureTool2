using System.Windows;
using SignatureTool2.ViewModel;

namespace SignatureTool2.View
{
    public partial class PostActionsDialog : Window
    {
        public PostActionsDialog(SignatureModel model)
        {
            InitializeComponent();
            DataContext = model;
        }

        private void OnConfirm(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        public static void Show(SignatureModel model)
        {
            new PostActionsDialog(model).ShowDialog();
        }
    }
}
