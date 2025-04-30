using KdxDesigner.Models;
using KdxDesigner.ViewModels;

using System.Collections.Generic;
using System.Windows;

namespace KdxDesigner.Views
{
    public partial class IOSelectView : Window
    {
        public string? SelectedAddress => (DataContext as IOSelectViewModel)?.SelectedAddress;

        public IOSelectView(List<IO> candidates)
        {
            InitializeComponent();
            var viewModel = new IOSelectViewModel();
            viewModel.Load(candidates);
            DataContext = viewModel;
        }
    }
}