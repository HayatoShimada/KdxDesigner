using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KdxDesigner.Models;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace KdxDesigner.ViewModels
{
    public partial class IOSelectViewModel : ObservableObject
    {
        public class AddressItem
        {
            public string Display { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
        }

        [ObservableProperty]
        private ObservableCollection<AddressItem> addressItems = new();

        [ObservableProperty]
        private AddressItem? selectedItem;

        public string? SelectedAddress => SelectedItem?.Address;

        public void Load(List<IO> candidates)
        {
            AddressItems = new ObservableCollection<AddressItem>(
                candidates.Select(io => new AddressItem
                {
                    Display = $"{io.IOText} ({io.Address})",
                    Address = io.Address ?? string.Empty
                })
            );
        }

        [RelayCommand]
        private void Confirm()
        {
            if (SelectedItem != null)
            {
                //Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.IsActive)?.DialogResult = true;
            }
            else
            {
                MessageBox.Show("選択してください。", "確認", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.IsActive)?.Close();
        }
    }
}