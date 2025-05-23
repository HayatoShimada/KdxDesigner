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
using KdxDesigner.Utils;

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
        private List<MnemonicDeviceWithProcess> joinedProcessList = new();
        private List<MnemonicDeviceWithProcessDetail> joinedProcessDetailList = new();

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


        // メモリ設定ボタンが押されたときの処理
        // MemoryテーブルとMnemonicDeviceテーブルにデータを保存する
        // 処理の流れ：必要事項の入力確認→MnemonicDeviceテーブルにデータを保存→Memoryテーブルにデータを保存
        [RelayCommand]
        private void MemorySetting()
        {
            if (SelectedCycle == null || SelectedPlc == null || ProcessDeviceStartL == null || DetailDeviceStartL == null)
            {
                if (SelectedCycle == null)
                    MessageBox.Show("Cycleが選択されていません。");
                if (SelectedPlc == null)
                    MessageBox.Show("Plcが選択されていません。");
                if (ProcessDeviceStartL == null)
                    MessageBox.Show("ProcessDeviceStartLが入力されていません。");
                if (DetailDeviceStartL == null)
                    MessageBox.Show("DetailDeviceStartLが入力されていません。");
            }
            else
            {
                var mnemonicService = new MnemonicDeviceService(_repository);    // MnemonicDeviceServiceのインスタンス

                // 工程詳細の一覧を読み出し
                List<ProcessDetailDto> details = _repository.GetProcessDetailDtos()
                    .Where(d => d.CycleId == SelectedCycle.Id)
                    .ToList();

                // CYの一覧を読み出し
                List<CY> cylinder = _repository.GetCYs()
                    .Where(o => o.PlcId == SelectedPlc.Id)
                    .ToList();

                // Operationの一覧を読み出し
                var cylinderIds = cylinder.Select(c => c.Id).ToHashSet();
                List<Operation> operations = _repository.GetOperations()
                    .Where(o => o.CYId.HasValue && cylinderIds.Contains(o.CYId.Value))
                    .ToList();

                // プロセスの必要デバイスを保存
                mnemonicService.SaveMnemonicDeviceProcess(Processes.ToList(), ProcessDeviceStartL.Value, SelectedPlc.Id);
                mnemonicService.SaveMnemonicDeviceProcessDetail(details, DetailDeviceStartL.Value, SelectedPlc.Id);

                // MnemonicId = 1 だとProcessニモニックのレコード
                var devices = mnemonicService.GetMnemonicDevice(SelectedCycle?.PlcId ?? throw new InvalidOperationException("SelectedCycle is null."));
                var devicesP = devices
                    .Where(m => m.MnemonicId == 1)
                    .ToList();
                var devicesD = devices
                    .Where(m => m.MnemonicId == 2)
                    .ToList();

                // Memoryテーブルにデータを保存
                var memoryService = new MemoryService(_repository);
                MessageBox.Show("Process情報をMemoryテーブルにデータを保存します。");
                foreach (var device in devicesP)
                {
                    bool result = memoryService.SaveMnemonicMemories(device);
                    if (!result)
                    {
                        MessageBox.Show("Memoryテーブルの保存に失敗しました。");
                        return;
                    }
                }
                MessageBox.Show("ProcessDetail情報をMemoryテーブルにデータを保存します。");
                foreach (var device in devicesD)
                {
                    bool result = memoryService.SaveMnemonicMemories(device);
                    if (!result)
                    {
                        MessageBox.Show("Memoryテーブルの保存に失敗しました。");
                        return;
                    }
                }

                MessageBox.Show("完了しました");
            }
        }


        // 出力処理ボタンが押されたときの処理
        // Cycleが選択されていない場合はエラーメッセージを表示
        [RelayCommand]
        private void ProcessOutput()
        {
            if (SelectedCycle == null)
            {
                MessageBox.Show("Cycleが選択されていません。");
                return;
            }
            if (SelectedPlc == null)
            {
                MessageBox.Show("PLCが選択されていません。");
                return;
            }
            if (ProcessDeviceStartL == null)
            {
                MessageBox.Show("ProcessDeviceStartLが入力されていません。");
                return;
            }
            if (DetailDeviceStartL == null)
            {
                MessageBox.Show("DetailDeviceStartLが入力されていません。");
                return;
            }
            if (Processes.Count == 0)
            {
                MessageBox.Show("Processが選択されていません。");
                return;
            }
            var mnemonicService = new MnemonicDeviceService(_repository);    // MnemonicDeviceServiceのインスタンス

            // detailsを取得
            var details = _repository.GetProcessDetailDtos();
            List<ProcessDetailDto> detailAll = new();
            foreach (var pros in Processes)
            {
                detailAll.AddRange(details.Where(d => d.ProcessId == pros.Id));
            }

            // IO一覧を取得
            var ioList = _repository.GetIoList();
            var memoryService = new MemoryService(_repository);
            var memoryList = memoryService.GetMemories(SelectedPlc.Id);

            // MnemonicId = 1 だとProcessニモニックのレコード
            var devices = mnemonicService.GetMnemonicDevice(SelectedCycle?.PlcId ?? throw new InvalidOperationException("SelectedCycle is null."));
            var devicesP = devices
                .Where(m => m.MnemonicId == 1)
                .ToList();
            var devicesD = devices
                .Where(m => m.MnemonicId == 2)
                .ToList();

            // MnemonicDeviceとProcessのリストを結合
            // 並び順はProcess.Idで昇順
            joinedProcessList = devicesP
                        .Join(
                            Processes.ToList(),
                            m => m.RecordId,
                            p => p.Id,
                            (m, p) => new MnemonicDeviceWithProcess
                            {
                                Mnemonic = m,
                                Process = p
                            })
                        .OrderBy(m => m.Process.Id)
                        .ToList();

            // MnemonicDeviceとProcessDetailのリストを結合
            // 並び順はProcessDetail.Idで昇順
            joinedProcessDetailList = devicesD
                    .Join(
                        detailAll.ToList(),
                        m => m.RecordId,
                        d => d.Id,
                        (m, d) => new MnemonicDeviceWithProcessDetail
                        {
                            Mnemonic = m,
                            Detail = d
                        })
                    .OrderBy(m => m.Detail.Id)
                    .ToList();

            // CSV出力処理
            // \Utils\ProcessBuilder.cs
            var outputRows = ProcessBuilder.GenerateAllLadderCsvRows(
                    SelectedCycle,
                    ProcessDeviceStartL!.Value,
                    DetailDeviceStartL!.Value,
                    joinedProcessList,
                    joinedProcessDetailList,
                    ioList,
                    out var errors
                );

            // 仮：結果をログ出力（実際にはCSVに保存などを検討）
            foreach (var row in outputRows)
            {
                Debug.WriteLine($"{row.Command} {row.Address}");
            }

            MessageBox.Show("出力処理が完了しました。");
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
