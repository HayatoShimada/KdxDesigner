// ViewModel: PlcSelectionViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services;
using KdxDesigner.Services.Access;
using KdxDesigner.Services.Error;

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
        private readonly IAccessRepository? _repository;
        private readonly MnemonicDeviceService? _mnemonicService;
        private readonly MnemonicTimerDeviceService? _timerService;
        private readonly ErrorService? _errorService;
        private readonly ProsTimeDeviceService? _prosTimeService;
        private readonly MnemonicSpeedDeviceService? _speedService; // クラス名が不明なため仮定
        private readonly MemoryService? _memoryService;

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

        [ObservableProperty] private int processDeviceStartL = 2000;
        [ObservableProperty] private int detailDeviceStartL = 3000;
        [ObservableProperty] private int operationDeviceStartM = 20000;
        [ObservableProperty] private int cylinderDeviceStartM = 30000;
        [ObservableProperty] private int cylinderDeviceStartD = 30000;
        [ObservableProperty] private int errorDeviceStartM = 52000;
        [ObservableProperty] private int deviceStartT = 2000;
        [ObservableProperty] private int prosTimeStartZR = 10000;
        [ObservableProperty] private int prosTimePreviousStartZR = 20000;
        [ObservableProperty] private int cyTimeStartZR = 30000;
        [ObservableProperty] private string valveSearchText = "SV";

        [ObservableProperty] private bool isProcessMemory = false;
        [ObservableProperty] private bool isDetailMemory = false;
        [ObservableProperty] private bool isOperationMemory = false;
        [ObservableProperty] private bool isCylinderMemory = false;
        [ObservableProperty] private bool isErrorMemory = false;
        [ObservableProperty] private bool isTimerMemory = false;
        [ObservableProperty] private bool isProsTimeMemory = false;
        [ObservableProperty] private bool isCyTimeMemory = false;

        [ObservableProperty] private bool isProcessOutput = false;
        [ObservableProperty] private bool isDetailOutput = false;
        [ObservableProperty] private bool isOperationOutput = false;
        [ObservableProperty] private bool isCylinderOutput = false;


        [ObservableProperty] private int memoryProgressMax;
        [ObservableProperty] private int memoryProgressValue;
        [ObservableProperty] private string memoryStatusMessage = string.Empty;
        [ObservableProperty] private List<OutputError> outputErrors = new();

        private List<ProcessDetailDto> allDetails = new();
        private List<Models.Process> allProcesses = new();

        public MainViewModel()
        {
            try
            {
                // 1. パス管理クラスを使ってDBパスを取得
                var pathManager = new DatabasePathManager();
                string dbPath = pathManager.ResolveDatabasePath();

                // 2. 接続文字列を生成
                string connectionString = pathManager.CreateConnectionString(dbPath);

                // 3. リポジトリに接続文字列を渡してインスタンス化
                _repository = new AccessRepository(connectionString);
                if (_repository.GetCompanies().Count == 0)
                {
                    MessageBox.Show("データベースに会社情報がありません。", "初期化エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                    return;
                }
                _mnemonicService = new MnemonicDeviceService(_repository);
                _timerService = new MnemonicTimerDeviceService(_repository);
                _errorService = new ErrorService(_repository);
                _prosTimeService = new ProsTimeDeviceService(_repository);
                _speedService = new MnemonicSpeedDeviceService(_repository); // クラス名が不明なため仮定
                _memoryService = new MemoryService(_repository);

                LoadInitialData();
            }
            catch (Exception ex)
            {
                // パス選択キャンセルなどで例外が発生した場合の処理
                MessageBox.Show(ex.Message, "初期化エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                // アプリケーションを終了する
                Application.Current.Shutdown();
                return;
            }
            
        }

        // データの更新
        #region Properties for Selected Operations
        private void LoadInitialData()
        {
            Companies = new ObservableCollection<Company>(_repository!.GetCompanies());
            allProcesses = _repository.GetProcesses();
            allDetails = _repository.GetProcessDetailDtos();
        }

        partial void OnSelectedCompanyChanged(Company? value)
        {
            if (value == null) return;
            Models = new ObservableCollection<Model>(_repository!.GetModels().Where(m => m.CompanyId == value.Id));
            SelectedModel = null;
        }

        partial void OnSelectedModelChanged(Model? value)
        {
            if (value == null) return;
            Plcs = new ObservableCollection<PLC>(_repository!.GetPLCs().Where(p => p.ModelId == value.Id));
            SelectedPlc = null;
        }

        partial void OnSelectedPlcChanged(PLC? value)
        {
            if (value == null) return;
            Cycles = new ObservableCollection<Cycle>(_repository!.GetCycles().Where(c => c.PlcId == value.Id));
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
                var op = _repository!.GetOperationById(selected.OperationId.Value);
                if (op != null)
                {
                    SelectedOperations.Clear();
                    SelectedOperations.Add(op);
                }
            }
        }

        #endregion

        // その他ボタン処理
        #region Properties for Process Details
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
                _repository!.UpdateOperation(op);
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
        #endregion

        // 出力処理
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
                // 1. エラー集約サービスのインスタンス化
                var errorAggregator = new ErrorAggregator();
                var ioAddressService = new IOAddressService(errorAggregator, _repository!);
                OutputErrors.Clear();

                // 2. データ準備
                MemoryStatusMessage = "データ準備中...";
                var (data, prepErrors) = PrepareDataForOutput();
                errorAggregator.AddErrors(prepErrors);

                // 3. ラダー初期化
                MemoryStatusMessage = "ラダー生成中...";
                var allOutputRows = new List<LadderCsvRow>();




                // 各ビルダーに ViewModel と エラー集約サービスを渡してインスタンス化
                // (ProcessBuilder, DetailBuilder, OperationBuilder も同様に修正されていると仮定)
                //var processBuilder = new ProcessBuilder(errorAggregator);
                var processDetailBuilder = new ProcessDetailBuilder(this, errorAggregator, ioAddressService);
                var operationBuilder = new OperationBuilder(this, errorAggregator, ioAddressService);
                var cylinderBuilder = new CylinderBuilder(this, errorAggregator, ioAddressService);


                var processRows = ProcessBuilder.GenerateAllLadderCsvRows(SelectedCycle!, ProcessDeviceStartL, DetailDeviceStartL, data.JoinedProcessList, data.JoinedProcessDetailList, data.IoList, out var processErrors);
                allOutputRows.AddRange(processRows);

                var detailRows = processDetailBuilder.GenerateAllLadderCsvRows(data.JoinedProcessList, data.JoinedProcessDetailList, data.JoinedOperationList, data.JoinedCylinderList, data.IoList);
                allOutputRows.AddRange(detailRows);

                var operationRows = operationBuilder.GenerateLadder(
                    data.JoinedProcessDetailList, 
                    data.JoinedOperationList, 
                    data.JoinedCylinderList, 
                    data.JoinedOperationWithTimerList, 
                    data.SpeedDevice, 
                    data.MnemonicErrors, 
                    data.ProsTime, 
                    data.IoList);
                allOutputRows.AddRange(operationRows);

                var cylinderRows = cylinderBuilder.GenerateLadder(
                    data.JoinedProcessDetailList, 
                    data.JoinedOperationList, 
                    data.JoinedCylinderList,
                    data.JoinedOperationWithTimerList, 
                    data.SpeedDevice, 
                    data.MnemonicErrors, 
                    data.ProsTime, 
                    data.IoList);
                allOutputRows.AddRange(cylinderRows);

                // 4. 集約されたエラーを取得してUIに反映
                OutputErrors = new List<OutputError>(errorAggregator.GetAllErrors());
                if (OutputErrors.Any())
                {
                    MessageBox.Show("ラダー生成中にエラーが検出されました。エラーリストを確認してください。", "生成エラー");
                }

                // 5. CSVエクスポート
                if (!OutputErrors.Any(e => e.IsCritical)) // IsCriticalのようなプロパティをOutputErrorに追加して判断するのも良い
                {
                    MemoryStatusMessage = "CSVファイル出力中...";
                    string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KdxLadder.csv");
                    LadderCsvExporter.ExportLadderCsv(allOutputRows, csvPath);
                    MemoryStatusMessage = "出力処理が完了しました。";
                    MessageBox.Show(MemoryStatusMessage);
                }
                else
                {
                    MemoryStatusMessage = "クリティカルなエラーのため、CSV出力を中止しました。";
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"出力処理中に致命的なエラーが発生しました: {ex.Message}";
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

            var devices = _mnemonicService!.GetMnemonicDevice(plcId);
            var timers = _repository!.GetTimersByCycleId(cycleId);
            var operations = _repository.GetOperations();
            var cylinders = _repository.GetCYs().Where(c => c.PlcId == plcId).ToList();
            var details = _repository.GetProcessDetailDtos().Where(d => d.CycleId == cycleId).ToList();
            var ioList = _repository.GetIoList();

            var devicesP = devices.Where(m => m.MnemonicId == (int)MnemonicType.Process).ToList();
            var devicesD = devices.Where(m => m.MnemonicId == (int)MnemonicType.ProcessDetail).ToList();
            var devicesO = devices.Where(m => m.MnemonicId == (int)MnemonicType.Operation).ToList();
            var devicesC = devices.Where(m => m.MnemonicId == (int)MnemonicType.CY).ToList();

            var timerDevices = _timerService!.GetMnemonicTimerDevice(plcId, cycleId);
            var prosTime = _prosTimeService!.GetProsTimeByMnemonicId(plcId, (int)MnemonicType.Operation);
            var speedDevice = _speedService!.GetMnemonicSpeedDevice(plcId);
            var mnemonicErrors = _errorService!.GetErrors(plcId, cycleId, (int)MnemonicType.Operation);

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

        
        // メモリ設定
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

            List<ProcessDetailDto> details = _repository!.GetProcessDetailDtos().Where(d => d.CycleId == SelectedCycle.Id).ToList();
            List<CY> cylinders = _repository!.GetCYs().Where(o => o.PlcId == SelectedPlc.Id).ToList();
            var operationIds = details.Select(c => c.OperationId).ToHashSet();
            List<Operation> operations = _repository.GetOperations().Where(o => operationIds.Contains(o.Id)).ToList();
            var ioList = _repository!.GetIoList();
            var timers = _repository!.GetTimersByCycleId(SelectedCycle.Id);

            return (details, cylinders, operations, ioList, timers);
        }

        // Mnemonic* と Timer* テーブルへのデータ保存をまとめたヘルパー
        private void SaveMnemonicAndTimerDevices((List<ProcessDetailDto> details, List<CY> cylinders, List<Operation> operations, List<IO> ioList, List<Models.Timer> timers) prepData)
        {
            MemoryStatusMessage = "ニーモニックデバイス情報を保存中...";
            _mnemonicService!.SaveMnemonicDeviceProcess(Processes.ToList(), ProcessDeviceStartL, SelectedPlc!.Id);
            _mnemonicService!.SaveMnemonicDeviceProcessDetail(prepData.details, DetailDeviceStartL, SelectedPlc!.Id);
            _mnemonicService!.SaveMnemonicDeviceOperation(prepData.operations, OperationDeviceStartM, SelectedPlc!.Id);
            _mnemonicService!.SaveMnemonicDeviceCY(prepData.cylinders, CylinderDeviceStartM, SelectedPlc!.Id);

            int timerCount = 0;
            _timerService!.SaveWithOperation(prepData.timers, prepData.operations, DeviceStartT, SelectedPlc!.Id, SelectedCycle!.Id, out timerCount);
            _timerService!.SaveWithCY(prepData.timers, prepData.cylinders, DeviceStartT, SelectedPlc!.Id, SelectedCycle!.Id, ref timerCount);

            _errorService!.SaveMnemonicDeviceOperation(prepData.operations, prepData.ioList, ErrorDeviceStartM, SelectedPlc!.Id, SelectedCycle!.Id);
            _prosTimeService!.SaveProsTime(prepData.operations, ProsTimeStartZR, ProsTimePreviousStartZR, CyTimeStartZR, SelectedPlc!.Id);
            _speedService!.Save(prepData.cylinders, CylinderDeviceStartD, SelectedPlc!.Id);
        }

        // Memoryテーブルへの保存処理
        private async Task SaveMemoriesToMemoryTableAsync((List<ProcessDetailDto> details, List<CY> cylinders, List<Operation> operations, List<IO> ioList, List<Models.Timer> timers) prepData)
        {
            var devices = _mnemonicService!.GetMnemonicDevice(SelectedPlc!.Id);
            var timerDevices = _timerService!.GetMnemonicTimerDevice(SelectedPlc!.Id, SelectedCycle!.Id);

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
            if (!await ProcessAndSaveMemoryAsync(IsProcessMemory, devicesP, _memoryService!.SaveMnemonicMemories, "Process")) return;
            if (!await ProcessAndSaveMemoryAsync(IsDetailMemory, devicesD, _memoryService!.SaveMnemonicMemories, "ProcessDetail")) return;
            if (!await ProcessAndSaveMemoryAsync(IsOperationMemory, devicesO, _memoryService!.SaveMnemonicMemories, "Operation")) return;
            if (!await ProcessAndSaveMemoryAsync(IsCylinderMemory, devicesC, _memoryService!.SaveMnemonicMemories, "CY")) return;
            if (!await ProcessAndSaveMemoryAsync(IsErrorMemory, devicesC, _memoryService!.SaveMnemonicMemories, "エラー")) return; // ★

            if (IsTimerMemory)
            {
                if (!await ProcessAndSaveMemoryAsync(true, timerDevices, _memoryService!.SaveMnemonicTimerMemoriesT, "Timer (T)")) return;
                if (!await ProcessAndSaveMemoryAsync(true, timerDevices, _memoryService!.SaveMnemonicTimerMemoriesZR, "Timer (ZR)")) return;
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
