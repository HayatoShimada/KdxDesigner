using KdxDesigner.Models;
using KdxDesigner.ViewModels;

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace KdxDesigner.Views
{
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            DataContext = new MainViewModel(); // ← これがあるか？
        }

        private void ProcessGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var selected = ProcessGrid.SelectedItems.Cast<Process>().ToList();
                vm.UpdateSelectedProcesses(selected);
            }
        }


        private void DetailGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var selected = (sender as DataGrid)?.SelectedItem as ProcessDetailDto;
                if (selected != null)
                {
                    vm.OnProcessDetailSelected(selected);
                }
            }
        }
    }
}
