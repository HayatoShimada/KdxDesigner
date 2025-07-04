using KdxDesigner.Services.Access;
using KdxDesigner.ViewModels;

using System.Windows;

namespace KdxDesigner.Views
{
    public partial class IoEditorView : Window
    {
        public IoEditorView(IAccessRepository repository)
        {
            InitializeComponent();
            DataContext = new IoEditorViewModel(repository);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}