// ViewModel: PlcSelectionViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services;
using KdxDesigner.Utils;
using KdxDesigner.Views;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace KdxDesigner.ViewModels
{

    public partial class MainViewModel : ObservableObject

    {
        private readonly AccessRepository _repository = new();
        private readonly MnemonicDeviceService _mnemonicService;
        private readonly MnemonicTimerDeviceService _timerService;
        private readonly ErrorService _errorService;
        private readonly ProsTimeDeviceService _prosTimeService;
        private readonly MnemonicSpeedDeviceService _speedService; // クラス名が不明なため仮定
        private readonly MemoryService _memoryService;

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

        [ObservableProperty] private int? processDeviceStartL = 2000;
        [ObservableProperty] private int? detailDeviceStartL = 3000;
        [ObservableProperty] private int? operationDeviceStartM = 20000;
        [ObservableProperty] private int? cylinderDeviceStartM = 30000;
        [ObservableProperty] private int? cylinderDeviceStartD = 30000;
        [ObservableProperty] private int? errorDeviceStartM = 52000;
        [ObservableProperty] private int? deviceStartT = 2000;
        [ObservableProperty] private int? prosTimeStartZR = 10000;
        [ObservableProperty] private int? prosTimePreviousStartZR = 20000;
        [ObservableProperty] private int? cyTimeStartZR = 30000;

        [ObservableProperty] private string? valveSearchText = "SV";
        [ObservableProperty] private bool isProcessMemory = false;
        [ObservableProperty] private bool isDetailMemory = false;
        [ObservableProperty] private bool isOperationMemory = false;
        [ObservableProperty] private bool isCylinderMemory = false;
        [ObservableProperty] private bool isErrorMemory = false;
        [ObservableProperty] private bool isTimerMemory = false;
        [ObservableProperty] private bool isProsTimeMemory = false;
        [ObservableProperty] private bool isCyTimeMemory = false;

        // メモリ保存処理における進捗バーの最大値（デバイスの総件数を設定） kuni            
        [ObservableProperty] private int memoryProgressMax;
        // メモリ保存処理における現在の進捗値（保存済みの件数）kuni
        [ObservableProperty] private int memoryProgressValue;
        // メモリ保存処理の進行状況を表示するテキスト（例：「Process保存中」「保存完了」など）kuni
        [ObservableProperty] private string memoryStatusMessage = string.Empty;
        [ObservableProperty] private List<OutputError> outputErrors = new();

        private List<ProcessDetailDto> allDetails = new();
        private List<Models.Process> allProcesses = new();
        private List<MnemonicDeviceWithProcess> joinedProcessList = new();
        private List<MnemonicDeviceWithProcessDetail> joinedProcessDetailList = new();
        private List<MnemonicDeviceWithOperation> joinedOperationList = new();
        private List<MnemonicDeviceWithCylinder> joinedCylinderList = new();
        private List<MnemonicTimerDeviceWithOperation> joinedOperationWithTimerList = new();

        public MainViewModel()
        {
            _repository = new AccessRepository();
            _mnemonicService = new MnemonicDeviceService(_repository);
            _timerService = new MnemonicTimerDeviceService(_repository);
            _errorService = new ErrorService(_repository);
            _prosTimeService = new ProsTimeDeviceService(_repository);
            _speedService = new MnemonicSpeedDeviceService(_repository); // クラス名が不明なため仮定
            _memoryService = new MemoryService(_repository);
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
        public void UpdateSelectedProcesses(List<Models.Process> selectedProcesses)
        {
            var selectedIds = selectedProcesses.Select(p => p.Id).ToHashSet();
            var filtered = allDetails
                .Where(d => d.ProcessId.HasValue && selectedIds.Contains(d.ProcessId.Value))
                .ToList();

            ProcessDetails = new ObservableCollection<ProcessDetailDto>(filtered);
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

        // 出力処理ボタンが押されたときの処理
        // Cycleが選択されていない場合はエラーメッセージを表示
        #region ProcessOutput

        [RelayCommand]
        private void ProcessOutput()
        {
            var errorMessages = ValidateProcessOutput();
            if (errorMessages.Any())
            {
                MessageBox.Show(string.Join("\n", errorMessages), "入力エラー");
                return;
            }

            try
            {
                // 1. データ準備
                MemoryStatusMessage = "データ準備中...";
                var (data, errors) = PrepareDataForOutput();
                OutputErrors = new List<OutputError>(errors);

                // 2. ラダー生成
                MemoryStatusMessage = "ラダー生成中...";
                var allOutputRows = new List<LadderCsvRow>();

                // 各ビルダーを呼び出してラダー行を生成し、結果とエラーを集約
                var processRows = ProcessBuilder.GenerateAllLadderCsvRows(SelectedCycle!, ProcessDeviceStartL!.Value, DetailDeviceStartL!.Value, data.JoinedProcessList, data.JoinedProcessDetailList, data.IoList, out var processErrors);
                allOutputRows.AddRange(processRows);
                OutputErrors.AddRange(processErrors);

                var detailRows = ProcessDetailBuilder.GenerateAllLadderCsvRows(data.JoinedProcessList, data.JoinedProcessDetailList, data.JoinedOperationList, data.JoinedCylinderList, data.IoList, out var detailErrors);
                allOutputRows.AddRange(detailRows);
                OutputErrors.AddRange(detailErrors);

                var operationRows = OperationBuilder.GenerateAllLadderCsvRows(data.JoinedProcessDetailList, data.JoinedOperationList, data.JoinedCylinderList, data.JoinedOperationWithTimerList, data.SpeedDevice, data.MnemonicErrors, data.ProsTime, data.IoList, SelectedPlc!.Id, out var operationErrors);
                allOutputRows.AddRange(operationRows);
                OutputErrors.AddRange(operationErrors);

                var cylinderBuilder = new CylinderBuilder(this); // 'this' を渡す必要性は要検討
                var cylinderRows = cylinderBuilder.GenerateAllLadderCsvRows(data.JoinedProcessDetailList, data.JoinedOperationList, data.JoinedCylinderList, data.JoinedOperationWithTimerList, data.SpeedDevice, data.MnemonicErrors, data.ProsTime, data.IoList, SelectedPlc!.Id, out var cylinderErrors);
                allOutputRows.AddRange(cylinderRows);
                OutputErrors.AddRange(cylinderErrors);

                // 3. CSVエクスポート
                MemoryStatusMessage = "CSVファイル出力中...";
                string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Test.csv");
                LadderCsvExporter.ExportLadderCsv(allOutputRows, csvPath);

                MemoryStatusMessage = "出力処理が完了しました。";
                MessageBox.Show(MemoryStatusMessage);
            }
            catch (Exception ex)
            {
                var errorMessage = $"出力処理中にエラーが発生しました: {ex.Message}";
                MemoryStatusMessage = errorMessage;
                MessageBox.Show(errorMessage, "エラー");
                Debug.WriteLine(ex);
            }
        }

        private List<string> ValidateProcessOutput()
        {
            var errors = new List<string>();
            if (SelectedCycle == null) errors.Add("Cycleが選択されていません。");
            if (SelectedPlc == null) errors.Add("PLCが選択されていません。");
            if (ProcessDeviceStartL == null) errors.Add("ProcessDeviceStartLが入力されていません。");
            if (DetailDeviceStartL == null) errors.Add("DetailDeviceStartLが入力されていません。");
            if (CyTimeStartZR == null) errors.Add("CyTimeStartZRが入力されていません。"); // 元のコードのチェック条件を反映
            if (Processes.Count == 0) errors.Add("Processが選択されていません。");
            return errors;
        }

        private ((List<MnemonicDeviceWithProcess> JoinedProcessList,
                  List<MnemonicDeviceWithProcessDetail> JoinedProcessDetailList,
                  List<MnemonicDeviceWithOperation> JoinedOperationList,
                  List<MnemonicDeviceWithCylinder> JoinedCylinderList,
                  List<MnemonicTimerDeviceWithOperation> JoinedOperationWithTimerList,
                  List<MnemonicSpeedDevice> SpeedDevice,
                  List<Error> MnemonicErrors,
                  List<ProsTime> ProsTime,
                  List<IO> IoList) Data, List<OutputError> Errors) PrepareDataForOutput()
        {
            var plcId = SelectedPlc!.Id;
            var cycleId = SelectedCycle!.Id;

            var devices = _mnemonicService.GetMnemonicDevice(plcId);
            var timers = _repository.GetTimersByCycleId(cycleId);
            var operations = _repository.GetOperations();
            var cylinders = _repository.GetCYs().Where(c => c.PlcId == plcId).ToList();
            var details = _repository.GetProcessDetailDtos().Where(d => d.CycleId == cycleId).ToList();
            var ioList = _repository.GetIoList();

            var devicesP = devices.Where(m => m.MnemonicId == (int)MnemonicType.Process).ToList();
            var devicesD = devices.Where(m => m.MnemonicId == (int)MnemonicType.ProcessDetail).ToList();
            var devicesO = devices.Where(m => m.MnemonicId == (int)MnemonicType.Operation).ToList();
            var devicesC = devices.Where(m => m.MnemonicId == (int)MnemonicType.CY).ToList();

            var timerDevices = _timerService.GetMnemonicTimerDevice(plcId, cycleId);
            var prosTime = _prosTimeService.GetProsTimeByMnemonicId(plcId, (int)MnemonicType.Operation);
            var speedDevice = _speedService.GetMnemonicSpeedDevice(plcId);
            var mnemonicErrors = _errorService.GetErrors(plcId, cycleId, (int)MnemonicType.Operation);

            // JOIN処理
            var joinedProcessList = devicesP.Join(Processes, m => m.RecordId, p => p.Id, (m, p) => new MnemonicDeviceWithProcess { Mnemonic = m, Process = p }).OrderBy(x => x.Process.Id).ToList();
            var joinedProcessDetailList = devicesD.Join(details, m => m.RecordId, d => d.Id, (m, d) => new MnemonicDeviceWithProcessDetail { Mnemonic = m, Detail = d }).OrderBy(x => x.Detail.Id).ToList();
            var joinedOperationList = devicesO.Join(operations, m => m.RecordId, o => o.Id, (m, o) => new MnemonicDeviceWithOperation { Mnemonic = m, Operation = o }).OrderBy(x => x.Operation.Id).ToList();
            var joinedCylinderList = devicesC.Join(cylinders, m => m.RecordId, c => c.Id, (m, c) => new MnemonicDeviceWithCylinder { Mnemonic = m, Cylinder = c }).OrderBy(x => x.Cylinder.Id).ToList();
            var joinedOperationWithTimerList = timerDevices.Join(operations, m => m.RecordId, o => o.Id, (m, o) => new MnemonicTimerDeviceWithOperation { Timer = m, Operation = o }).OrderBy(x => x.Operation.Id).ToList();

            var dataTuple = (joinedProcessList, joinedProcessDetailList, joinedOperationList, joinedCylinderList, joinedOperationWithTimerList, speedDevice, mnemonicErrors, prosTime, ioList);
            return (dataTuple, new List<OutputError>()); // 初期エラーリスト
        }

        #endregion

        

        #region MemorySetting

        [RelayCommand]
        private async Task MemorySetting()
        {
            if (!ValidateMemorySettings()) return;

            // 3. データ準備
            var prepData = PrepareDataForMemorySetting();

            // 4. Mnemonic/Timerテーブルへの事前保存
            if (prepData == null)
            {
                // データ準備に失敗した場合、ユーザーに通知して処理を中断
                MessageBox.Show("データ準備に失敗しました。CycleまたはPLCが選択されているか確認してください。", "エラー");
                return;
            }

            SaveMnemonicAndTimerDevices(prepData.Value);
            await SaveMemoriesToMemoryTableAsync(prepData.Value);
        }

        private bool ValidateMemorySettings()
        {
            var errorMessages = new List<string>();
            if (SelectedCycle == null) errorMessages.Add("Cycleが選択されていません。");
            if (SelectedPlc == null) errorMessages.Add("PLCが選択されていません。");
            if (ProcessDeviceStartL == null) errorMessages.Add("ProcessDeviceStartLが入力されていません。");
            if (DetailDeviceStartL == null) errorMessages.Add("DetailDeviceStartLが入力されていません。");
            if (OperationDeviceStartM == null) errorMessages.Add("OperationDeviceStartMが入力されていません。");
            if (CylinderDeviceStartM == null) errorMessages.Add("CylinderDeviceStartMが入力されていません。");
            if (DeviceStartT == null) errorMessages.Add("DeviceStartTが入力されていません。");
            if (ErrorDeviceStartM == null) errorMessages.Add("ErrorStartMが入力されていません。");
            if (ProsTimeStartZR == null) errorMessages.Add("ProsTimeStartZRが入力されていません。");
            if (ProsTimePreviousStartZR == null) errorMessages.Add("ProsTimePreviousStartZRが入力されていません。");
            if (CyTimeStartZR == null) errorMessages.Add("CyTimeStartZRが入力されていません。");
            if (CylinderDeviceStartD == null) errorMessages.Add("CylinderDeviceStartDが入力されていません。");

            if (errorMessages.Any())
            {
                MessageBox.Show(string.Join("\n", errorMessages), "入力エラー");
                return false;
            }
            return true;
        }

        // MemorySettingに必要なデータを準備するヘルパー
        private (List<ProcessDetailDto> details, List<CY> cylinders, List<Operation> operations, List<IO> ioList, List<Models.Timer> timers)? PrepareDataForMemorySetting()
        {
            if (SelectedCycle == null || SelectedPlc == null) return null;

            List<ProcessDetailDto> details = _repository.GetProcessDetailDtos().Where(d => d.CycleId == SelectedCycle.Id).ToList();
            List<CY> cylinders = _repository.GetCYs().Where(o => o.PlcId == SelectedPlc.Id).ToList();
            var operationIds = details.Select(c => c.OperationId).ToHashSet();
            List<Operation> operations = _repository.GetOperations().Where(o => operationIds.Contains(o.Id)).ToList();
            var ioList = _repository.GetIoList();
            var timers = _repository.GetTimersByCycleId(SelectedCycle.Id);

            return (details, cylinders, operations, ioList, timers);
        }

        // Mnemonic* と Timer* テーブルへのデータ保存をまとめたヘルパー
        private void SaveMnemonicAndTimerDevices((List<ProcessDetailDto> details, List<CY> cylinders, List<Operation> operations, List<IO> ioList, List<Models.Timer> timers) prepData)
        {
            MemoryStatusMessage = "ニーモニックデバイス情報を保存中...";
            _mnemonicService.SaveMnemonicDeviceProcess(Processes.ToList(), ProcessDeviceStartL!.Value, SelectedPlc!.Id);
            _mnemonicService.SaveMnemonicDeviceProcessDetail(prepData.details, DetailDeviceStartL!.Value, SelectedPlc!.Id);
            _mnemonicService.SaveMnemonicDeviceOperation(prepData.operations, OperationDeviceStartM!.Value, SelectedPlc!.Id);
            _mnemonicService.SaveMnemonicDeviceCY(prepData.cylinders, CylinderDeviceStartM!.Value, SelectedPlc!.Id);

            int timerCount = 0;
            _timerService.SaveWithOperation(prepData.timers, prepData.operations, DeviceStartT!.Value, SelectedPlc!.Id, SelectedCycle!.Id, out timerCount);
            _timerService.SaveWithCY(prepData.timers, prepData.cylinders, DeviceStartT!.Value, SelectedPlc!.Id, SelectedCycle!.Id, ref timerCount);

            _errorService.SaveMnemonicDeviceOperation(prepData.operations, prepData.ioList, ErrorDeviceStartM!.Value, SelectedPlc!.Id, SelectedCycle!.Id);
            _prosTimeService.SaveProsTime(prepData.operations, ProsTimeStartZR!.Value, ProsTimePreviousStartZR!.Value, CyTimeStartZR!.Value, SelectedPlc!.Id);
            _speedService.Save(prepData.cylinders, CylinderDeviceStartD!.Value, SelectedPlc!.Id);
        }

        // Memoryテーブルへの保存処理
        private async Task SaveMemoriesToMemoryTableAsync((List<ProcessDetailDto> details, List<CY> cylinders, List<Operation> operations, List<IO> ioList, List<Models.Timer> timers) prepData)
        {
            var devices = _mnemonicService.GetMnemonicDevice(SelectedPlc!.Id);
            var timerDevices = _timerService.GetMnemonicTimerDevice(SelectedPlc!.Id, SelectedCycle!.Id);

            var devicesP = devices.Where(m => m.MnemonicId == (int)MnemonicType.Process).ToList();
            var devicesD = devices.Where(m => m.MnemonicId == (int)MnemonicType.ProcessDetail).ToList();
            var devicesO = devices.Where(m => m.MnemonicId == (int)MnemonicType.Operation).ToList();
            var devicesC = devices.Where(m => m.MnemonicId == (int)MnemonicType.CY).ToList();

            // ★注意: IsErrorMemory の処理対象が devicesC になっています。これは意図通りでしょうか？
            // errorDevices のような別のリストを使うべきかもしれません。現状は元のコードのままにしています。

            MemoryProgressMax = (IsProcessMemory ? devicesP.Count : 0) +
                                (IsDetailMemory ? devicesD.Count : 0) +
                                (IsOperationMemory ? devicesO.Count : 0) +
                                (IsCylinderMemory ? devicesC.Count : 0) +
                                (IsErrorMemory ? devicesC.Count : 0) + // ★
                                (IsTimerMemory ? timerDevices.Count * 2 : 0);
            MemoryProgressValue = 0;

            // 汎用ヘルパーを使って繰り返しを削減
            if (!await ProcessAndSaveMemoryAsync(IsProcessMemory, devicesP, _memoryService.SaveMnemonicMemories, "Process")) return;
            if (!await ProcessAndSaveMemoryAsync(IsDetailMemory, devicesD, _memoryService.SaveMnemonicMemories, "ProcessDetail")) return;
            if (!await ProcessAndSaveMemoryAsync(IsOperationMemory, devicesO, _memoryService.SaveMnemonicMemories, "Operation")) return;
            if (!await ProcessAndSaveMemoryAsync(IsCylinderMemory, devicesC, _memoryService.SaveMnemonicMemories, "CY")) return;
            if (!await ProcessAndSaveMemoryAsync(IsErrorMemory, devicesC, _memoryService.SaveMnemonicMemories, "エラー")) return; // ★

            if (IsTimerMemory)
            {
                if (!await ProcessAndSaveMemoryAsync(true, timerDevices, _memoryService.SaveMnemonicTimerMemoriesT, "Timer (T)")) return;
                if (!await ProcessAndSaveMemoryAsync(true, timerDevices, _memoryService.SaveMnemonicTimerMemoriesZR, "Timer (ZR)")) return;
            }

            MemoryStatusMessage = "保存完了！";
            MessageBox.Show("すべてのメモリ保存が完了しました。");
        }

        // Memory保存の繰り返し処理を共通化するヘルパー
        private async Task<bool> ProcessAndSaveMemoryAsync<T>(bool shouldProcess, IEnumerable<T> devices, Func<T, bool> saveAction, string categoryName)
        {
            if (!shouldProcess) return true;

            MessageBox.Show($"{categoryName}情報をMemoryテーブルにデータを保存します。", "確認");
            MemoryStatusMessage = $"{categoryName}情報を保存中...";

            foreach (var device in devices)
            {
                bool result = await Task.Run(() => saveAction(device));
                if (!result)
                {
                    MemoryStatusMessage = $"Memoryテーブル（{categoryName}）の保存に失敗しました。";
                    MessageBox.Show(MemoryStatusMessage, "エラー");
                    return false;
                }
                MemoryProgressValue++;
            }
            return true;
        }

        #endregion

    }
}
