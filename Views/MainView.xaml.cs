using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.ViewModels;

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace KdxDesigner.Views
{
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            DataContext = App.Services!.GetRequiredService<MainViewModel>(); // 修正: 'App.Services' を型名でアクセス  
        }

        private void ProcessGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                // 新規行のプレースホルダーを除外してProcessオブジェクトのみを取得
                var selected = ProcessGrid.SelectedItems
                    .OfType<Process>()  // Cast<Process>()の代わりにOfType<Process>()を使用
                    .ToList();
                vm.UpdateSelectedProcesses(selected);
            }
        }

        private void DetailGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var selected = (sender as DataGrid)?.SelectedItem as ProcessDetail;
                if (selected != null)
                {
                    vm.OnProcessDetailSelected(selected);
                }
            }
        }

        private void NumberOnlyTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 数字（0～9）のみ許可  
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }
    }
}
