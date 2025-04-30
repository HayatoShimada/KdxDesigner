using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KdxDesigner.Data;
using KdxDesigner.Models;
using KdxDesigner.Views;
using KdxDesigner.Utils;

using System.Collections.ObjectModel;

namespace KdxDesigner.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly AccessRepository _repository;

        [ObservableProperty]
        private ObservableCollection<Process> processes = new();

        [ObservableProperty]
        private ObservableCollection<ProcessDetailDto> processDetails = new();

        [ObservableProperty]
        private int selectedCount;

        private List<ProcessDetailDto> allDetails = new(); // キャッシュ保持

        public MainViewModel()
        {
            _repository = new AccessRepository("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=KDX_Designer.accdb;");
            LoadData();
        }

        private void LoadData()
        {
            Processes = new ObservableCollection<Process>(_repository.GetProcesses());
            allDetails = _repository.GetProcessDetailDtos();
        }

        private void UpdateSelectedCount()
        {
            SelectedCount = Processes.Count(p => p.IsSelected);
        }


        [RelayCommand]
        private void FilterProcessDetails()
        {
            var selectedIds = Processes
                .Where(p => p.IsSelected)
                .Select(p => p.Id)
                .ToHashSet();

            var filtered = allDetails
                .Where(d => d.ProcessId.HasValue && selectedIds.Contains(d.ProcessId.Value))
                .ToList();

            ProcessDetails = new ObservableCollection<ProcessDetailDto>(filtered);
        }

        [RelayCommand]
        public void DeselectAllProcesses()
        {
            foreach (var process in Processes)
            {
                process.IsSelected = false;
            }
            UpdateSelectedCount();
        }

        [RelayCommand]
        public void SelectAllProcesses()
        {
            foreach (var process in Processes)
            {
                process.IsSelected = true;
            }
            UpdateSelectedCount();
        }

    }
}
