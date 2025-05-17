// ViewModel: PlcSelectionViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KdxDesigner.Services;
using KdxDesigner.Models;
using KdxDesigner.Views;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using KdxDesigner.Utils.Process;

namespace KdxDesigner.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly AccessRepository _repository = new();

        [ObservableProperty] private ObservableCollection<Company> companies = new();
        [ObservableProperty] private ObservableCollection<Model> models = new();
        [ObservableProperty] private ObservableCollection<PLC> plcs = new();
        [ObservableProperty] private ObservableCollection<Cycle> cycles = new();
        [ObservableProperty] private ObservableCollection<Models.Process> processes = new();
        [ObservableProperty] private ObservableCollection<ProcessDetailDto> processDetails = new();
        [ObservableProperty] private ObservableCollection<Operation> selectedOperations = new();

        [ObservableProperty] private Company? selectedCompany;
        [ObservableProperty] private Model? selectedModel;
        [ObservableProperty] private PLC? selectedPlc;
        [ObservableProperty] private Cycle? selectedCycle;
        [ObservableProperty] private Models.Process? selectedProcess;

        [ObservableProperty] private int? processDeviceStartL;
        [ObservableProperty] private int? detailDeviceStartL;


        [ObservableProperty]
        private ObservableCollection<OutputError> outputErrors = new();

        private List<ProcessDetailDto> allDetails = new();
        private List<Models.Process> allProcesses = new();

        public MainViewModel()
        {
            LoadInitialData();
        }

        private void LoadInitialData()
        {
            Companies = new ObservableCollection<Company>(_repository.GetCompanies());
            allProcesses = _repository.GetProcesses();
            allDetails = _repository.GetProcessDetailDtos();
        }

        partial void OnSelectedCompanyChanged(Company? value)
        {
            if (value == null) return;
            Models = new ObservableCollection<Model>(_repository.GetModels().Where(m => m.CompanyId == value.Id));
            SelectedModel = null;
        }

        partial void OnSelectedModelChanged(Model? value)
        {
            if (value == null) return;
            Plcs = new ObservableCollection<PLC>(_repository.GetPLCs().Where(p => p.ModelId == value.Id));
            SelectedPlc = null;
        }

        partial void OnSelectedPlcChanged(PLC? value)
        {
            if (value == null) return;
            Cycles = new ObservableCollection<Cycle>(_repository.GetCycles().Where(c => c.PlcId == value.Id));
            SelectedCycle = null;
        }

        partial void OnSelectedCycleChanged(Cycle? value)
        {
            if (value == null)
            {
                Processes = new ObservableCollection<Models.Process>(allProcesses);
                return;
            }
            Processes = new ObservableCollection<Models.Process>(allProcesses.Where(p => p.CycleId == value.Id));
        }

        [RelayCommand]
        public void UpdateSelectedProcesses(List<Models.Process> selectedProcesses)
        {
            var selectedIds = selectedProcesses.Select(p => p.Id).ToHashSet();
            var filtered = allDetails
                .Where(d => d.ProcessId.HasValue && selectedIds.Contains(d.ProcessId.Value))
                .ToList();

            ProcessDetails = new ObservableCollection<ProcessDetailDto>(filtered);
        }


        public void OnProcessDetailSelected(ProcessDetailDto selected)
        {
            if (selected?.OperationId != null)
            {
                var op = _repository.GetOperationById(selected.OperationId.Value);
                if (op != null)
                {
                    SelectedOperations.Clear();
                    SelectedOperations.Add(op);
                }
            }
        }

        [RelayCommand]
        private void SaveOperation()
        {
            foreach (var op in SelectedOperations)
            {
                _repository.UpdateOperation(op);
            }
            MessageBox.Show("保存しました。");
        }

        [RelayCommand]
        private void OpenSettings()
        {
            var view = new SettingsView();  // Views/SettingsView.xaml
            view.ShowDialog();              // モーダルで表示
        }

        [RelayCommand]
        private void ProcessOutput()
        {
            if (SelectedCycle == null || SelectedPlc == null)
            {
                MessageBox.Show("Cycleが選択されていません。");
            }
            else
            {
                // IO一覧を取得
                var ioList = _repository.GetIoList();
                // 
                var memoryList = _repository.GetMemories(SelectedPlc.Id);

                if (SelectedCycle != null)
                {
                    var outputRows = ProcessBuilder.GenerateAllLadderCsvRows(
                        SelectedCycle,
                        Processes.ToList(),
                        ProcessDetails.ToList(),
                        ioList,
                        out var errors
                    );

                    // 仮：結果をログ出力（実際にはCSVに保存などを検討）
                    foreach (var row in outputRows)
                    {
                        Debug.WriteLine($"{row.Command} {row.Address}");
                    }

                    OutputErrors = new ObservableCollection<OutputError>(errors);

                    MessageBox.Show("出力処理が完了しました。");
                }
                else
                {
                    MessageBox.Show("Cycleが選択されていません。");
                }
            }
            return;
        }

        [RelayCommand]
        private void OpenMemoryEditor()
        {
            if (SelectedPlc == null)
            {
                MessageBox.Show("PLCを選択してください。");
                return;
            }

            var view = new MemoryEditorView(SelectedPlc.Id);
            view.ShowDialog();
        }

    }
}
