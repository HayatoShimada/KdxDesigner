// ViewModel: PlcSelectionViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services;
using KdxDesigner.Services.Access;
using KdxDesigner.Services.Error;
using KdxDesigner.Services.ErrorService;
using KdxDesigner.Services.IOAddress;
using KdxDesigner.Services.IOSelector;
using KdxDesigner.Services.MemonicTimerDevice;
using KdxDesigner.Services.Memory;
using KdxDesigner.Services.MnemonicDevice;
using KdxDesigner.Services.MnemonicSpeedDevice;
using KdxDesigner.Services.ProsTimeDevice;
using KdxDesigner.Utils;
using KdxDesigner.Views;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace KdxDesigner.ViewModels
{

    public partial class MainViewModel : ObservableObject
    {
        protected private readonly IAccessRepository? _repository;
        protected private readonly MnemonicDeviceService? _mnemonicService;
        protected private readonly MnemonicTimerDeviceService? _timerService;
        protected private readonly ErrorService? _errorService;
        protected private readonly ProsTimeDeviceService? _prosTimeService;
        protected private readonly MnemonicSpeedDeviceService? _speedService; // クラス名が不明なため仮定
        protected private readonly MemoryService? _memoryService;
        protected private readonly WpfIOSelectorService? _ioSelectorService;
        
        // 開いているProcessFlowDetailWindowのリスト
        private readonly List<Window> _openProcessFlowWindows = new();

        [ObservableProperty] private ObservableCollection<Company> companies = new();
        [ObservableProperty] private ObservableCollection<Model> models = new();
        [ObservableProperty] private ObservableCollection<PLC> plcs = new();
        [ObservableProperty] private ObservableCollection<Cycle> cycles = new();
        [ObservableProperty] private ObservableCollection<Models.Process> processes = new();
        [ObservableProperty] private ObservableCollection<ProcessDetail> processDetails = new();
        [ObservableProperty] private ObservableCollection<Operation> selectedOperations = new();
        [ObservableProperty] private ObservableCollection<ProcessCategory> processCategories = new();
        [ObservableProperty] private ObservableCollection<ProcessDetailCategory> processDetailCategories = new();

        [ObservableProperty] private Company? selectedCompany;
        [ObservableProperty] private Model? selectedModel;
        [ObservableProperty] private PLC? selectedPlc;
        [ObservableProperty] private Cycle? selectedCycle;
        [ObservableProperty] private Models.Process? selectedProcess;
        [ObservableProperty] private ProcessDetail? selectedProcessDetail;

        // ﾗｲﾝ
        //[ObservableProperty] private int processDeviceStartL = 14000;
        //[ObservableProperty] private int detailDeviceStartL = 15000;
        //[ObservableProperty] private int operationDeviceStartM = 20000;
        //[ObservableProperty] private int cylinderDeviceStartM = 30000;
        //[ObservableProperty] private int cylinderDeviceStartD = 5000;
        //[ObservableProperty] private int errorDeviceStartM = 120000;
        //[ObservableProperty] private int errorDeviceStartT = 2000;
        //[ObservableProperty] private int deviceStartT = 0;
        //[ObservableProperty] private int timerStartZR = 3000;
        //[ObservableProperty] private int prosTimeStartZR = 12000;
        //[ObservableProperty] private int prosTimePreviousStartZR = 24000;
        //[ObservableProperty] private int cyTimeStartZR = 30000;

        // 上型造型機
        [ObservableProperty] private int processDeviceStartL = 14300;
        [ObservableProperty] private int detailDeviceStartL = 17000;
        [ObservableProperty] private int operationDeviceStartM = 26000;
        [ObservableProperty] private int cylinderDeviceStartM = 50000;
        [ObservableProperty] private int cylinderDeviceStartD = 5200;
        [ObservableProperty] private int errorDeviceStartM = 121500;
        [ObservableProperty] private int errorDeviceStartT = 3500;
        [ObservableProperty] private int deviceStartT = 0;
        [ObservableProperty] private int timerStartZR = 3000;
        [ObservableProperty] private int prosTimeStartZR = 14000;
        [ObservableProperty] private int prosTimePreviousStartZR = 26000;
        [ObservableProperty] private int cyTimeStartZR = 32000;

        // 下型造型機
        //[ObservableProperty] private int processDeviceStartL = 14500;
        //[ObservableProperty] private int detailDeviceStartL = 18000;
        //[ObservableProperty] private int operationDeviceStartM = 28000;
        //[ObservableProperty] private int cylinderDeviceStartM = 52000;
        //[ObservableProperty] private int cylinderDeviceStartD = 5300;
        //[ObservableProperty] private int errorDeviceStartM = 121700;
        //[ObservableProperty] private int errorDeviceStartT = 3700;
        //[ObservableProperty] private int deviceStartT = 0;
        //[ObservableProperty] private int timerStartZR = 3000;
        //[ObservableProperty] private int prosTimeStartZR = 15000;
        //[ObservableProperty] private int prosTimePreviousStartZR = 27000;
        //[ObservableProperty] private int cyTimeStartZR = 33000;

        // 上型型交換
        //[ObservableProperty] private int processDeviceStartL = 14700;
        //[ObservableProperty] private int detailDeviceStartL = 19000;
        //[ObservableProperty] private int operationDeviceStartM = 28000;
        //[ObservableProperty] private int cylinderDeviceStartM = 52000;
        //[ObservableProperty] private int cylinderDeviceStartD = 5300;
        //[ObservableProperty] private int errorDeviceStartM = 121700;
        //[ObservableProperty] private int errorDeviceStartT = 3700;
        //[ObservableProperty] private int deviceStartT = 0;
        //[ObservableProperty] private int timerStartZR = 3000;
        //[ObservableProperty] private int prosTimeStartZR = 15000;
        //[ObservableProperty] private int prosTimePreviousStartZR = 27000;
        //[ObservableProperty] private int cyTimeStartZR = 33000;

        // 下型型交換
        //[ObservableProperty] private int processDeviceStartL = 14500;
        //[ObservableProperty] private int detailDeviceStartL = 18000;
        //[ObservableProperty] private int operationDeviceStartM = 28000;
        //[ObservableProperty] private int cylinderDeviceStartM = 52000;
        //[ObservableProperty] private int cylinderDeviceStartD = 5300;
        //[ObservableProperty] private int errorDeviceStartM = 121700;
        //[ObservableProperty] private int errorDeviceStartT = 3700;
        //[ObservableProperty] private int deviceStartT = 0;
        //[ObservableProperty] private int timerStartZR = 3000;
        //[ObservableProperty] private int prosTimeStartZR = 15000;
        //[ObservableProperty] private int prosTimePreviousStartZR = 27000;
        //[ObservableProperty] private int cyTimeStartZR = 33000;

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
        [ObservableProperty] private bool isDebug = false;

        [ObservableProperty] private int memoryProgressMax;
        [ObservableProperty] private int memoryProgressValue;
        [ObservableProperty] private string memoryStatusMessage = string.Empty;
        [ObservableProperty] private List<OutputError> outputErrors = new();

        private List<ProcessDetail> allDetails = new();
        private List<Models.Process> allProcesses = new();
        public List<Servo> selectedServo = new(); // 選択されたサーボのリスト
        
        // 変更追跡用のバックアップ
        private List<Models.Process> originalProcesses = new();
        private List<ProcessDetail> originalProcessDetails = new();
        private List<Operation> originalOperations = new();

        public MainViewModel()
        {
            try
            {
                // 1. パス管理と接続文字列生成
                var pathManager = new DatabasePathManager();
                string dbPath = pathManager.ResolveDatabasePath();
                string connectionString = pathManager.CreateConnectionString(dbPath);

                _repository = new AccessRepository(connectionString);
                _mnemonicService = new MnemonicDeviceService(_repository);
                _timerService = new MnemonicTimerDeviceService(_repository, this);
                _errorService = new ErrorService(_repository);
                _prosTimeService = new ProsTimeDeviceService(_repository);
                _speedService = new MnemonicSpeedDeviceService(_repository);
                _memoryService = new MemoryService(_repository);
                _ioSelectorService = new WpfIOSelectorService();

                // 3. データベースの基本的な健全性チェック
                if (_repository.GetCompanies().Count == 0)
                {
                    // データがない場合はエラーメッセージを表示して終了
                    MessageBox.Show("データベースは有効ですが、必須の会社情報が登録されていません。", "初期化エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                    return; // ここで終了しても、フィールドは既に初期化済み
                }

                // 4. 全ての初期化が成功した後に、データをロード
                LoadInitialData();
                
                // DataGridの編集完了イベントを処理するためのコマンドを初期化
                InitializeEditCommands();
            }
            catch (Exception ex)
            {
                // ファイルが見つからない、選択がキャンセルされた等の致命的なエラー
                MessageBox.Show(ex.Message, "初期化エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                // この時点でフィールドはnullのままなので、後続の処理はガード句で保護される
            }
        }
        
        private void InitializeEditCommands()
        {
            // 編集完了時の自動保存を有効化
            Processes.CollectionChanged += (s, e) => SaveProcessChanges();
            ProcessDetails.CollectionChanged += (s, e) => SaveProcessDetailChanges();
            SelectedOperations.CollectionChanged += (s, e) => SaveOperationChanges();
        }
        
        private void SaveProcessChanges()
        {
            if (_repository == null) return;
            // 変更されたProcessを保存
            foreach (var process in Processes)
            {
                try
                {
                    if (process.Id == 0)
                    {
                        // 新規追加
                        _repository.InsertProcess(process);
                    }
                    else
                    {
                        // 更新
                        _repository.UpdateProcess(process);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Process保存エラー: {ex.Message}");
                }
            }
        }
        
        private void SaveProcessDetailChanges()
        {
            if (_repository == null) return;
            // 変更されたProcessDetailを保存
            foreach (var detail in ProcessDetails)
            {
                try
                {
                    if (detail.Id == 0)
                    {
                        // 新規追加
                        _repository.InsertProcessDetail(detail);
                    }
                    else
                    {
                        // 更新
                        _repository.UpdateProcessDetail(detail);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ProcessDetail保存エラー: {ex.Message}");
                }
            }
        }
        
        private void SaveOperationChanges()
        {
            if (_repository == null) return;
            // 変更されたOperationを保存
            foreach (var operation in SelectedOperations)
            {
                try
                {
                    if (operation.Id == 0)
                    {
                        // 新規追加
                        _repository.InsertOperation(operation);
                    }
                    else
                    {
                        // 更新
                        _repository.UpdateOperation(operation);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Operation保存エラー: {ex.Message}");
                }
            }
        }
        
        // 個別行の保存メソッド
        public void SaveProcessRow(Models.Process? process)
        {
            if (_repository == null || process == null) return;
            try
            {
                if (process.Id == 0)
                {
                    // 新規追加
                    _repository.InsertProcess(process);
                    // データを再読み込み
                    if (SelectedCycle != null)
                    {
                        Processes = new ObservableCollection<Models.Process>(
                            _repository.GetProcesses().Where(p => p.CycleId == SelectedCycle.Id).OrderBy(p => p.SortNumber));
                    }
                }
                else
                {
                    // 更新
                    _repository.UpdateProcess(process);
                }
                MessageBox.Show("工程を保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Process保存エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        public void SaveProcessDetailRow(ProcessDetail? detail)
        {
            if (_repository == null || detail == null) return;
            try
            {
                if (detail.Id == 0)
                {
                    // 新規追加
                    _repository.InsertProcessDetail(detail);
                    // データを再読み込み
                    allDetails = _repository.GetProcessDetails();
                    if (SelectedProcess != null)
                    {
                        UpdateSelectedProcesses(new List<Models.Process> { SelectedProcess });
                    }
                }
                else
                {
                    // 更新
                    _repository.UpdateProcessDetail(detail);
                }
                MessageBox.Show("工程詳細を保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ProcessDetail保存エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        public void SaveOperationRow(Operation? operation)
        {
            if (_repository == null || operation == null) return;
            try
            {
                if (operation.Id == 0)
                {
                    // 新規追加
                    _repository.InsertOperation(operation);
                    // 選択されたOperationsを再読み込み
                    if (SelectedProcessDetail != null)
                    {
                        OnProcessDetailSelected(SelectedProcessDetail);
                    }
                }
                else
                {
                    // 更新
                    _repository.UpdateOperation(operation);
                }
                MessageBox.Show("操作を保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Operation保存エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecute()
        {
            if (_repository == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return false;
            }
            return true;
        }

        // データの更新
        #region Properties for Selected Operations
        private void LoadInitialData()
        {
            if (_repository == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            Companies = new ObservableCollection<Company>(_repository.GetCompanies());
            allProcesses = _repository.GetProcesses();
            allDetails = _repository.GetProcessDetails();
            
            // カテゴリを読み込む
            var processCategories = _repository.GetProcessCategories();
            if (processCategories.Count == 0)
            {
                // ProcessCategoryテーブルが存在しないか空の場合、デフォルトのカテゴリを作成
                processCategories = new List<ProcessCategory>
                {
                    new ProcessCategory { Id = 1, ProcessCategoryName = "標準工程" },
                    new ProcessCategory { Id = 2, ProcessCategoryName = "準備工程" },
                    new ProcessCategory { Id = 3, ProcessCategoryName = "加工工程" },
                    new ProcessCategory { Id = 4, ProcessCategoryName = "検査工程" },
                    new ProcessCategory { Id = 5, ProcessCategoryName = "その他" }
                };
            }
            ProcessCategories = new ObservableCollection<ProcessCategory>(processCategories);
            ProcessDetailCategories = new ObservableCollection<ProcessDetailCategory>(_repository.GetProcessDetailCategories());

            // 設定ファイルを読み込む
            SettingsManager.Load();

            // メモリプロファイルを読み込む
            LoadMemoryProfile();

            // 前回の選択を復元
            if (SettingsManager.Settings.LastSelectedCompanyId.HasValue)
            {
                var savedCompany = Companies.FirstOrDefault(c => c.Id == SettingsManager.Settings.LastSelectedCompanyId.Value);
                if (savedCompany != null)
                {
                    SelectedCompany = savedCompany;

                    // モデルも復元
                    if (SettingsManager.Settings.LastSelectedModelId.HasValue)
                    {
                        var savedModel = Models.FirstOrDefault(m => m.Id == SettingsManager.Settings.LastSelectedModelId.Value);
                        if (savedModel != null)
                        {
                            SelectedModel = savedModel;

                            // サイクルも復元
                            if (SettingsManager.Settings.LastSelectedCycleId.HasValue)
                            {
                                var savedCycle = Cycles.FirstOrDefault(c => c.Id == SettingsManager.Settings.LastSelectedCycleId.Value);
                                if (savedCycle != null)
                                {
                                    SelectedCycle = savedCycle;
                                }
                            }
                        }
                    }
                }
            }
        }

        partial void OnSelectedCompanyChanged(Company? value)
        {
            if (!CanExecute()) return;

            if (value == null) return;
            Models = new ObservableCollection<Model>(_repository!.GetModels().Where(m => m.CompanyId == value.Id));
            SelectedModel = null;

            // 選択した会社IDを保存
            SettingsManager.Settings.LastSelectedCompanyId = value.Id;
            SettingsManager.Save();
        }

        partial void OnSelectedModelChanged(Model? value)
        {
            if (!CanExecute()) return;

            if (value == null) return;
            Plcs = new ObservableCollection<PLC>(_repository!.GetPLCs().Where(p => p.ModelId == value.Id));
            SelectedPlc = null;

            // 選択したモデルIDを保存
            SettingsManager.Settings.LastSelectedModelId = value.Id;
            SettingsManager.Save();
        }

        partial void OnSelectedPlcChanged(PLC? value)
        {
            if (!CanExecute()) return;

            if (value == null) return;
            Cycles = new ObservableCollection<Cycle>(_repository!.GetCycles().Where(c => c.PlcId == value.Id));
            SelectedCycle = null;
        }

        partial void OnSelectedCycleChanged(Cycle? value)
        {
            if (!CanExecute()) return;

            if (value == null) return;
            Processes = new ObservableCollection<Models.Process>(
                allProcesses.Where(p => p.CycleId == value.Id).OrderBy(p => p.SortNumber));

            // バックアップコレクションを更新
            UpdateBackupCollections();
            
            // 選択したサイクルIDを保存
            SettingsManager.Settings.LastSelectedCycleId = value.Id;
            SettingsManager.Save();
        }
        public void OnProcessDetailSelected(ProcessDetail selected)
        {
            if (_repository == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            if (selected?.OperationId != null)
            {
                var op = _repository.GetOperationById(selected.OperationId.Value);
                if (op != null)
                {
                    SelectedOperations.Clear();
                    SelectedOperations.Add(op);
                }
            }
            
            // SelectedOperationsが更新されたらバックアップも更新
            UpdateBackupCollections();
        }

        #endregion

        // 一括保存コマンド
        [RelayCommand]
        private void SaveAllChanges()
        {
            if (_repository == null)
            {
                MessageBox.Show("リポジトリが初期化されていません。", "エラー");
                return;
            }

            try
            {
                int savedCount = 0;
                
                // Processの保存
                foreach (var process in Processes)
                {
                    if (process.Id == 0)
                    {
                        _repository.InsertProcess(process);
                        savedCount++;
                    }
                    else if (HasChanges(process, originalProcesses))
                    {
                        _repository.UpdateProcess(process);
                        savedCount++;
                    }
                }
                
                // ProcessDetailの保存
                foreach (var detail in ProcessDetails)
                {
                    if (detail.Id == 0)
                    {
                        _repository.InsertProcessDetail(detail);
                        savedCount++;
                    }
                    else if (HasChanges(detail, originalProcessDetails))
                    {
                        _repository.UpdateProcessDetail(detail);
                        savedCount++;
                    }
                }
                
                // Operationの保存
                foreach (var operation in SelectedOperations)
                {
                    if (operation.Id == 0)
                    {
                        _repository.InsertOperation(operation);
                        savedCount++;
                    }
                    else if (HasChanges(operation, originalOperations))
                    {
                        _repository.UpdateOperation(operation);
                        savedCount++;
                    }
                }
                
                // 保存後、バックアップを更新
                UpdateBackupCollections();
                
                MessageBox.Show($"{savedCount}件のデータを保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存中にエラーが発生しました：{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        [RelayCommand]
        private void ResetChanges()
        {
            if (MessageBox.Show("編集内容を破棄してリセットしますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // データを再読み込み
                if (_repository != null && SelectedCycle != null)
                {
                    // Processesを再読み込み
                    allProcesses = _repository.GetProcesses();
                    Processes = new ObservableCollection<Models.Process>(
                        allProcesses.Where(p => p.CycleId == SelectedCycle.Id).OrderBy(p => p.SortNumber));
                    
                    // ProcessDetailsを再読み込み  
                    allDetails = _repository.GetProcessDetails();
                    ProcessDetails.Clear();
                    
                    // SelectedOperationsをクリア
                    SelectedOperations.Clear();
                    
                    // バックアップコレクションを更新
                    UpdateBackupCollections();
                }
            }
        }
        
        private bool HasChanges<T>(T current, List<T> original) where T : class
        {
            if (current is Models.Process process)
            {
                var originalProcess = original.OfType<Models.Process>().FirstOrDefault(p => p.Id == process.Id);
                if (originalProcess == null) return true;
                return !AreProcessesEqual(process, originalProcess);
            }
            else if (current is ProcessDetail detail)
            {
                var originalDetail = original.OfType<ProcessDetail>().FirstOrDefault(d => d.Id == detail.Id);
                if (originalDetail == null) return true;
                return !AreProcessDetailsEqual(detail, originalDetail);
            }
            else if (current is Operation operation)
            {
                var originalOperation = original.OfType<Operation>().FirstOrDefault(o => o.Id == operation.Id);
                if (originalOperation == null) return true;
                return !AreOperationsEqual(operation, originalOperation);
            }
            return false;
        }
        
        private bool AreProcessesEqual(Models.Process p1, Models.Process p2)
        {
            return p1.ProcessName == p2.ProcessName &&
                   p1.CycleId == p2.CycleId &&
                   p1.TestStart == p2.TestStart &&
                   p1.TestCondition == p2.TestCondition &&
                   p1.TestMode == p2.TestMode &&
                   p1.AutoMode == p2.AutoMode &&
                   p1.AutoStart == p2.AutoStart &&
                   p1.ProcessCategoryId == p2.ProcessCategoryId &&
                   p1.ILStart == p2.ILStart &&
                   p1.Comment1 == p2.Comment1 &&
                   p1.Comment2 == p2.Comment2 &&
                   p1.SortNumber == p2.SortNumber;
        }
        
        private bool AreProcessDetailsEqual(ProcessDetail d1, ProcessDetail d2)
        {
            return d1.DetailName == d2.DetailName &&
                   d1.ProcessId == d2.ProcessId &&
                   d1.OperationId == d2.OperationId &&
                   d1.StartSensor == d2.StartSensor &&
                   d1.CategoryId == d2.CategoryId &&
                   d1.FinishSensor == d2.FinishSensor &&
                   d1.BlockNumber == d2.BlockNumber &&
                   d1.SkipMode == d2.SkipMode &&
                   d1.CycleId == d2.CycleId &&
                   d1.SortNumber == d2.SortNumber &&
                   d1.Comment == d2.Comment &&
                   d1.ILStart == d2.ILStart &&
                   d1.StartTimerId == d2.StartTimerId;
        }
        
        private bool AreOperationsEqual(Operation o1, Operation o2)
        {
            return o1.OperationName == o2.OperationName &&
                   o1.CYId == o2.CYId &&
                   o1.CategoryId == o2.CategoryId &&
                   o1.Stay == o2.Stay &&
                   o1.GoBack == o2.GoBack &&
                   o1.Start == o2.Start &&
                   o1.Finish == o2.Finish &&
                   o1.Valve1 == o2.Valve1 &&
                   o1.S1 == o2.S1 &&
                   o1.S2 == o2.S2 &&
                   o1.S3 == o2.S3 &&
                   o1.S4 == o2.S4 &&
                   o1.S5 == o2.S5 &&
                   o1.SS1 == o2.SS1 &&
                   o1.SS2 == o2.SS2 &&
                   o1.SS3 == o2.SS3 &&
                   o1.SS4 == o2.SS4 &&
                   o1.PIL == o2.PIL &&
                   o1.SC == o2.SC &&
                   o1.FC == o2.FC &&
                   o1.CycleId == o2.CycleId &&
                   o1.SortNumber == o2.SortNumber &&
                   o1.Con == o2.Con;
        }
        
        private void UpdateBackupCollections()
        {
            originalProcesses = Processes.Select(p => new Models.Process
            {
                Id = p.Id,
                ProcessName = p.ProcessName,
                CycleId = p.CycleId,
                TestStart = p.TestStart,
                TestCondition = p.TestCondition,
                TestMode = p.TestMode,
                AutoMode = p.AutoMode,
                AutoStart = p.AutoStart,
                ProcessCategoryId = p.ProcessCategoryId,
                ILStart = p.ILStart,
                Comment1 = p.Comment1,
                Comment2 = p.Comment2,
                SortNumber = p.SortNumber
            }).ToList();
            
            originalProcessDetails = ProcessDetails.Select(d => new ProcessDetail
            {
                Id = d.Id,
                DetailName = d.DetailName,
                ProcessId = d.ProcessId,
                OperationId = d.OperationId,
                StartSensor = d.StartSensor,
                CategoryId = d.CategoryId,
                FinishSensor = d.FinishSensor,
                BlockNumber = d.BlockNumber,
                SkipMode = d.SkipMode,
                CycleId = d.CycleId,
                SortNumber = d.SortNumber,
                Comment = d.Comment,
                ILStart = d.ILStart,
                StartTimerId = d.StartTimerId
            }).ToList();
            
            originalOperations = SelectedOperations.Select(o => new Operation
            {
                Id = o.Id,
                OperationName = o.OperationName,
                CYId = o.CYId,
                CategoryId = o.CategoryId,
                Stay = o.Stay,
                GoBack = o.GoBack,
                Start = o.Start,
                Finish = o.Finish,
                Valve1 = o.Valve1,
                S1 = o.S1,
                S2 = o.S2,
                S3 = o.S3,
                S4 = o.S4,
                S5 = o.S5,
                SS1 = o.SS1,
                SS2 = o.SS2,
                SS3 = o.SS3,
                SS4 = o.SS4,
                PIL = o.PIL,
                SC = o.SC,
                FC = o.FC,
                CycleId = o.CycleId,
                SortNumber = o.SortNumber,
                Con = o.Con
            }).ToList();
        }

        // その他ボタン処理
        #region Properties for Process Details
        [RelayCommand]
        public void UpdateSelectedProcesses(List<Models.Process> selectedProcesses)
        {
            var selectedIds = selectedProcesses.Select(p => p.Id).ToHashSet();
            var filtered = allDetails
                .Where(d => selectedIds.Contains(d.ProcessId))
                .ToList();

            ProcessDetails = new ObservableCollection<ProcessDetail>(filtered);
            
            // ProcessDetailsが更新されたらバックアップも更新
            UpdateBackupCollections();
        }

        [RelayCommand]
        private void OpenIoEditor()
        {
            if (_repository == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            // Viewにリポジトリのインスタンスを渡して生成
            var view = new IoEditorView(_repository, this);
            view.Show(); // モードレスダイアログとして表示
        }

        [RelayCommand]
        private void SaveOperation()
        {
            if (!CanExecute()) return;

            foreach (var op in SelectedOperations)
            {
                _repository!.UpdateOperation(op);
            }
            MessageBox.Show("保存しました。");
        }

        [RelayCommand]
        private void OpenSettings()
        {
            var view = new SettingsView();
            view.ShowDialog();
        }

        // 工程フローは工程フロー詳細に統一されたため、このメソッドは使用されません
        // [RelayCommand]
        // private void OpenProcessFlow()
        // {
        //     if (SelectedCycle == null)
        //     {
        //         MessageBox.Show("サイクルを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
        //         return;
        //     }
        //
        //     if (_repository == null)
        //     {
        //         MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
        //         return;
        //     }
        //
        //     viewModel.LoadProcessDetails(SelectedCycle.Id);
        //     
        //     var view = new ProcessFlowView(viewModel);
        //     
        //     // ウィンドウが閉じられたときにリストから削除
        //     view.Closed += (s, e) =>
        //     {
        //         if (s is Window w)
        //         {
        //             _openProcessFlowWindows.Remove(w);
        //         }
        //     };
        //     
        //     // リストに追加して非モーダルで表示
        //     _openProcessFlowWindows.Add(view);
        //     view.Show();
        // }
        
        [RelayCommand]
        private void OpenProcessFlowDetail()
        {
            if (SelectedCycle == null)
            {
                MessageBox.Show("サイクルを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_repository == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            // 新しいウィンドウを作成
            var window = new ProcessFlowDetailWindow(_repository, SelectedCycle.Id, SelectedCycle.CycleName ?? $"サイクル{SelectedCycle.Id}");
            
            // ウィンドウが閉じられたときにリストから削除
            window.Closed += (s, e) =>
            {
                if (s is Window w)
                {
                    _openProcessFlowWindows.Remove(w);
                }
            };
            
            // リストに追加して表示
            _openProcessFlowWindows.Add(window);
            window.Show();
        }
        
        [RelayCommand]
        private void CloseAllProcessFlowWindows()
        {
            // すべてのProcessFlowDetailWindowを閉じる
            var windowsToClose = _openProcessFlowWindows.ToList();
            foreach (var window in windowsToClose)
            {
                window.Close();
            }
            _openProcessFlowWindows.Clear();
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

        [RelayCommand]
        private void OpenLinkDeviceManager()
        {
            if (_repository == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            // Viewにリポジトリのインスタンスを渡す
            var view = new LinkDeviceView(_repository);
            view.ShowDialog(); // モーダルダイアログとして表示
        }

        [RelayCommand]
        private void OpenTimerEditor()
        {
            if (_repository == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            var view = new TimerEditorView(_repository, this);
            view.ShowDialog();
        }

        [RelayCommand]
        private void OpenMemoryProfileManager()
        {
            if (_repository == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }
            var view = new MemoryProfileView(this, _repository);
            view.ShowDialog();
        }

        private void LoadMemoryProfile()
        {
            var profileManager = new MemoryProfileManager();
            MemoryProfile? profileToLoad = null;

            // 前回使用したプロファイルを取得
            if (!string.IsNullOrEmpty(SettingsManager.Settings.LastUsedMemoryProfileId))
            {
                profileToLoad = profileManager.GetProfile(SettingsManager.Settings.LastUsedMemoryProfileId);
            }

            // 前回のプロファイルがない場合はデフォルトプロファイルを使用
            if (profileToLoad == null)
            {
                profileToLoad = profileManager.GetDefaultProfile();
            }

            // プロファイルを適用
            if (profileToLoad != null)
            {
                profileManager.ApplyProfileToViewModel(profileToLoad, this);
            }
        }

        public void SaveLastUsedProfile(string profileId)
        {
            SettingsManager.Settings.LastUsedMemoryProfileId = profileId;
            SettingsManager.Save();
        }

        #endregion

        // 出力処理
        #region ProcessOutput
        [RelayCommand]
        private void ProcessOutput()
        {
            var errorMessages = ValidateProcessOutput();

            if (_repository == null || SelectedPlc == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }

            try
            {
                MemoryStatusMessage = "処理を開始します...";
                OutputErrors.Clear();
                var allGeneratedErrors = new List<OutputError>();
                var allOutputRows = new List<LadderCsvRow>();

                // --- 1. データ準備 ---
                MemoryStatusMessage = "データ準備中...";
                var (data, prepErrors) = PrepareDataForOutput();
                allGeneratedErrors.AddRange(prepErrors);

                // --- 2. 各ビルダーによるラダー生成 ---
                MemoryStatusMessage = "ラダー生成中...";

                // ProcessBuilder (out パラメータ方式を維持、または新しい方式に修正)
                var processRows = ProcessBuilder.GenerateAllLadderCsvRows(
                    SelectedCycle!, 
                    ProcessDeviceStartL, 
                    DetailDeviceStartL, 
                    data.JoinedProcessList, 
                    data.JoinedProcessDetailList, 
                    data.IoList, 
                    _repository!, 
                    out var processErrors);
                allOutputRows.AddRange(processRows);
                allGeneratedErrors.AddRange(processErrors);

                // ProcessDetailBuilder
                var pdErrorAggregator = new ErrorAggregator((int)KdxDesigner.Models.Define.MnemonicType.ProcessDetail);

                var pdIoAddressService = new IOAddressService(pdErrorAggregator, _repository, SelectedPlc.Id, _ioSelectorService);
                var detailBuilder = new ProcessDetailBuilder(this, pdErrorAggregator, pdIoAddressService, _repository);
                var detailRows = detailBuilder.GenerateAllLadderCsvRows(
                    data.JoinedProcessList, 
                    data.JoinedProcessDetailList, 
                    data.JoinedOperationList, 
                    data.JoinedCylinderList, 
                    data.IoList, 
                    data.JoinedProcessDetailWithTimerList);
                allOutputRows.AddRange(detailRows);
                allGeneratedErrors.AddRange(pdErrorAggregator.GetAllErrors());

                // OperationBuilder
                var opErrorAggregator = new ErrorAggregator((int)KdxDesigner.Models.Define.MnemonicType.Operation);
                var opIoAddressService = new IOAddressService(opErrorAggregator, _repository, SelectedPlc.Id, _ioSelectorService);
                var operationBuilder = new OperationBuilder(this, opErrorAggregator, opIoAddressService);
                var operationRows = operationBuilder.GenerateLadder(
                    data.JoinedProcessDetailList, 
                    data.JoinedOperationList, 
                    data.JoinedCylinderList, 
                    data.JoinedOperationWithTimerList, 
                    data.SpeedDevice, 
                    data.MnemonicErrors, 
                    data.ProsTime, data.IoList);
                allOutputRows.AddRange(operationRows);
                allGeneratedErrors.AddRange(opErrorAggregator.GetAllErrors());

                // CylinderBuilder
                var cyErrorAggregator = new ErrorAggregator((int)KdxDesigner.Models.Define.MnemonicType.CY);
                var cyIoAddressService = new IOAddressService(cyErrorAggregator, _repository, SelectedPlc.Id, _ioSelectorService);
                var cylinderBuilder = new CylinderBuilder(this, cyErrorAggregator, cyIoAddressService);
                var cylinderRows = cylinderBuilder.GenerateLadder(
                    data.JoinedProcessDetailList, 
                    data.JoinedOperationList, 
                    data.JoinedCylinderList, 
                    data.JoinedOperationWithTimerList, 
                    data.JoinedCylinderWithTimerList, 
                    data.SpeedDevice, 
                    data.MnemonicErrors, 
                    data.ProsTime, data.IoList);
                allOutputRows.AddRange(cylinderRows);
                allGeneratedErrors.AddRange(cyErrorAggregator.GetAllErrors());

                // --- 3. 全てのエラーをUIに一度に反映 ---
                OutputErrors = allGeneratedErrors.Distinct().ToList(); // 重複するエラーを除く場合


                if (OutputErrors.Any())
                {
                    MessageBox.Show("ラダー生成中にエラーが検出されました。エラーリストを確認してください。", "生成エラー");
                }

                // --- 4. CSVエクスポート ---
                if (OutputErrors.Any(e => e.IsCritical))
                {
                    MemoryStatusMessage = "致命的なエラーのため、CSV出力を中止しました。";
                    MessageBox.Show(MemoryStatusMessage, "エラー");
                    return;
                }
                ExportLadderCsvFile(processRows, "Process.csv", "全ラダー");
                ExportLadderCsvFile(detailRows, "Detail.csv", "全ラダー");
                ExportLadderCsvFile(operationRows, "Operation.csv", "全ラダー");
                ExportLadderCsvFile(cylinderRows, "Cylinder.csv", "全ラダー");
                ExportLadderCsvFile(allOutputRows, "KdxLadder_All.csv", "全ラダー");
                MessageBox.Show("出力処理が完了しました。", "完了");
            }
            catch (Exception ex)
            {
                var errorMessage = $"出力処理中に致命的なエラーが発生しました: {ex.Message}";
                var stackMessage = $"出力処理中に致命的なエラーが発生しました: {ex.StackTrace}";

                MemoryStatusMessage = errorMessage;
                MessageBox.Show(errorMessage, "エラー");
                MessageBox.Show(stackMessage, "エラー");

                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// CSVファイルのエクスポート処理を共通化するヘルパーメソッド
        /// </summary>
        public void ExportLadderCsvFile(List<LadderCsvRow> rows, string fileName, string categoryName)
        {
            if (!rows.Any()) return; // 出力する行がなければ何もしない

            try
            {
                MemoryStatusMessage = $"{categoryName} CSVファイル出力中...";
                string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                LadderCsvExporter.ExportLadderCsv(rows, csvPath);
            }
            catch (Exception ex)
            {

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
                  List<MnemonicTimerDeviceWithDetail> JoinedProcessDetailWithTimerList,
                  List<MnemonicDeviceWithOperation> JoinedOperationList,
                  List<MnemonicDeviceWithCylinder> JoinedCylinderList,
                  List<MnemonicTimerDeviceWithOperation> JoinedOperationWithTimerList,
                  List<MnemonicTimerDeviceWithCylinder> JoinedCylinderWithTimerList,
                  List<MnemonicSpeedDevice> SpeedDevice,
                  List<Error> MnemonicErrors,
                  List<ProsTime> ProsTime,
                  List<IO> IoList) Data, List<OutputError> Errors) PrepareDataForOutput()
        {

            if (_repository == null || SelectedPlc == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return (
                    (new List<MnemonicDeviceWithProcess>(),
                     new List<MnemonicDeviceWithProcessDetail>(),
                     new List<MnemonicTimerDeviceWithDetail>(),
                     new List<MnemonicDeviceWithOperation>(),
                     new List<MnemonicDeviceWithCylinder>(),
                     new List<MnemonicTimerDeviceWithOperation>(),
                     new List<MnemonicTimerDeviceWithCylinder>(),
                     new List<MnemonicSpeedDevice>(),
                     new List<Error>(),
                     new List<ProsTime>(),
                     new List<IO>()),
                    new List<OutputError>()
                );
            }

            var plcId = SelectedPlc!.Id;
            var cycleId = SelectedCycle!.Id;

            var devices = _mnemonicService!.GetMnemonicDevice(plcId);
            var timers = _repository.GetTimersByCycleId(cycleId);
            var operations = _repository.GetOperations();


            // 1. まずPlcIdで絞り込む
            var cylindersForPlc = _repository.GetCYs().Where(c => c.PlcId == plcId).OrderBy(c => c.SortNumber);

            // 2. さらに、ProcessStartCycle に cycleId が含まれるものでフィルタリング
            var filteredCylinders = cylindersForPlc
                .Where(c =>
                    // ProcessStartCycle が null や空でないことを最初に確認
                    !string.IsNullOrWhiteSpace(c.ProcessStartCycle) &&

                    // 文字列をセミコロンで分割し、空の要素は除去
                    c.ProcessStartCycle.Split(';', StringSplitOptions.RemoveEmptyEntries)

                    // 分割された各ID文字列のいずれか(Any)が cycleId と一致するかチェック
                    .Any(idString =>
                        // 安全に数値に変換し、cycleId と比較する
                        int.TryParse(idString.Trim(), out int parsedId) && parsedId == cycleId
                    )
                )
                .ToList().OrderBy(c => c.SortNumber);

            var cylinders = filteredCylinders;

            var details = _repository.GetProcessDetails().Where(d => d.CycleId == cycleId).ToList();
            var ioList = _repository.GetIoList();
            selectedServo = _repository.GetServos(null, null);

            var devicesP = devices.Where(m => m.MnemonicId == (int)KdxDesigner.Models.Define.MnemonicType.Process).ToList().OrderBy(p => p.StartNum);
            var devicesD = devices.Where(m => m.MnemonicId == (int)KdxDesigner.Models.Define.MnemonicType.ProcessDetail).ToList().OrderBy(d => d.StartNum);
            var devicesO = devices.Where(m => m.MnemonicId == (int)KdxDesigner.Models.Define.MnemonicType.Operation).ToList().OrderBy(o => o.StartNum);
            var devicesC = devices.Where(m => m.MnemonicId == (int)KdxDesigner.Models.Define.MnemonicType.CY).ToList().OrderBy(c => c.StartNum);

            var timerDevices = _timerService!.GetMnemonicTimerDevice(plcId, cycleId);
            var prosTime = _prosTimeService!.GetProsTimeByMnemonicId(plcId, (int)KdxDesigner.Models.Define.MnemonicType.Operation);

            var speedDevice = _speedService!.GetMnemonicSpeedDevice(plcId);
            var mnemonicErrors = _errorService!.GetErrors(plcId, cycleId, (int)KdxDesigner.Models.Define.MnemonicType.Operation);

            // JOIN処理
            var joinedProcessList = devicesP
                .Join(Processes, m => m.RecordId, p => p.Id, (m, p) 
                => new MnemonicDeviceWithProcess { Mnemonic = m, Process = p }).ToList();
            var joinedProcessDetailList = devicesD
                .Join(details, m => m.RecordId, d => d.Id, (m, d) 
                => new MnemonicDeviceWithProcessDetail { Mnemonic = m, Detail = d }).ToList();

            var timerDevicesDetail = timerDevices.Where(t => t.MnemonicId == (int)KdxDesigner.Models.Define.MnemonicType.ProcessDetail).ToList();

            var joinedProcessDetailWithTimerList = timerDevicesDetail.Join(
                details, m => m.RecordId, o => o.Id, (m, o) =>
                new MnemonicTimerDeviceWithDetail { Timer = m, Detail = o }).OrderBy(x => x.Detail.Id).ToList();

            var joinedOperationList = devicesO
                .Join(operations, m => m.RecordId, o => o.Id, (m, o) 
                => new MnemonicDeviceWithOperation { Mnemonic = m, Operation = o })
                .OrderBy(x => x.Mnemonic.StartNum).ToList();
            var joinedCylinderList = devicesC
                .Join(cylinders, m => m.RecordId, c => c.Id, (m, c) 
                => new MnemonicDeviceWithCylinder { Mnemonic = m, Cylinder = c })
                .OrderBy(x => x.Mnemonic.StartNum).ToList();

            var timerDevicesOperation = timerDevices.Where(t => t.MnemonicId == (int)KdxDesigner.Models.Define.MnemonicType.Operation).ToList();
            var joinedOperationWithTimerList = timerDevicesOperation
                .Join(operations, m => m.RecordId, o => o.Id, (m, o) 
                => new MnemonicTimerDeviceWithOperation { Timer = m, Operation = o })
                .OrderBy(x => x.Operation.SortNumber).ToList();

            var timerDevicesCY = timerDevices.Where(t => t.MnemonicId == (int)KdxDesigner.Models.Define.MnemonicType.CY).ToList();
            var joinedCylinderWithTimerList = timerDevicesCY
                .Join(cylinders, m => m.RecordId, o => o.Id, (m, o) 
                => new MnemonicTimerDeviceWithCylinder { Timer = m, Cylinder = o })
                .OrderBy(x => x.Cylinder.Id).ToList();

            var dataTuple = (
                joinedProcessList,
                joinedProcessDetailList,
                joinedProcessDetailWithTimerList,
                joinedOperationList,
                joinedCylinderList,
                joinedOperationWithTimerList,
                joinedCylinderWithTimerList,
                speedDevice,
                mnemonicErrors,
                prosTime,
                ioList);
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
        private (
            List<ProcessDetail> details, 
            List<CY> cylinders, 
            List<Operation> operations, 
            List<IO> ioList, 
            List<Models.Timer> timers)? PrepareDataForMemorySetting()
        {
            if (SelectedCycle == null || _repository == null || SelectedPlc == null || _ioSelectorService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return null;
            }

            List<ProcessDetail> details = _repository
                .GetProcessDetails()
                .Where(d => d.CycleId == SelectedCycle.Id).OrderBy(d => d.SortNumber).ToList();
            List<CY> cylinders = _repository.GetCYs().Where(o => o.PlcId == SelectedPlc.Id).OrderBy(c => c.SortNumber).ToList();

            var filteredCylinders = cylinders
                .Where(c =>
                    // ProcessStartCycle が null や空でないことを最初に確認
                    !string.IsNullOrWhiteSpace(c.ProcessStartCycle) &&

                    // 文字列をセミコロンで分割し、空の要素は除去
                    c.ProcessStartCycle.Split(';', StringSplitOptions.RemoveEmptyEntries)

                    // 分割された各ID文字列のいずれか(Any)が cycleId と一致するかチェック
                    .Any(idString =>
                        // 安全に数値に変換し、cycleId と比較する
                        int.TryParse(idString.Trim(), out int parsedId) && parsedId == SelectedCycle.Id
                    )
                )
                .ToList();


            var operationIds = details.Select(c => c.OperationId).ToHashSet();
            List<Operation> operations = _repository.GetOperations().ToList();
            
            var op = operations
                .Where(o => o.CycleId == SelectedCycle.Id)
                .OrderBy(o => o.SortNumber).ToList();
            var ioList = _repository.GetIoList();
            var timers = _repository.GetTimersByCycleId(SelectedCycle.Id);

            return (details, filteredCylinders, op, ioList, timers);
        }

        // Mnemonic* と Timer* テーブルへのデータ保存をまとめたヘルパー
        private void SaveMnemonicAndTimerDevices(
            (List<ProcessDetail> details, 
            List<CY> cylinders, 
            List<Operation> operations, List<IO> ioList, List<Models.Timer> timers) prepData)
        {
            MemoryStatusMessage = "ニーモニックデバイス情報を保存中...";
            _mnemonicService!.DeleteAllMnemonicDevices();
            _mnemonicService!.SaveMnemonicDeviceProcess(Processes.ToList(), ProcessDeviceStartL, SelectedPlc!.Id);
            _mnemonicService!.SaveMnemonicDeviceProcessDetail(prepData.details, DetailDeviceStartL, SelectedPlc!.Id);
            _mnemonicService!.SaveMnemonicDeviceOperation(prepData.operations, OperationDeviceStartM, SelectedPlc!.Id);
            _mnemonicService!.SaveMnemonicDeviceCY(prepData.cylinders, CylinderDeviceStartM, SelectedPlc!.Id);

            if (_repository == null || _timerService == null || _errorService == null || _prosTimeService == null || _speedService == null)
            {
                MessageBox.Show("システムの初期化が不完全なため、処理を実行できません。", "エラー");
                return;
            }
            var timer = _repository.GetTimers();
            var details = _repository.GetProcessDetails();
            var operations = _repository.GetOperations();
            var cylinders = _repository.GetCYs();

            int timerCount = 0;

            // Timerテーブルの保存
            _timerService!.DeleteAllMnemonicTimerDevice();
            _timerService!.SaveWithDetail(timer, details, DeviceStartT, SelectedPlc!.Id, ref timerCount);
            _timerService!.SaveWithOperation(timer, operations, DeviceStartT, SelectedPlc!.Id, ref timerCount);
            _timerService!.SaveWithCY(timer, cylinders, DeviceStartT, SelectedPlc!.Id, ref timerCount);

            // Errorテーブルの保存
            _errorService!.DeleteErrorTable();
            _errorService!.SaveMnemonicDeviceOperation(prepData.operations, prepData.ioList, ErrorDeviceStartM, ErrorDeviceStartT, SelectedPlc!.Id, SelectedCycle!.Id);

            // ProsTimeテーブルの保存
            _prosTimeService!.DeleteProsTimeTable();
            _prosTimeService!.SaveProsTime(prepData.operations, ProsTimeStartZR, ProsTimePreviousStartZR, CyTimeStartZR, SelectedPlc!.Id);

            // Speedテーブルの保存
            _speedService!.DeleteSpeedTable();
            _speedService!.Save(prepData.cylinders, CylinderDeviceStartD, SelectedPlc!.Id);
        }

        // Memoryテーブルへの保存処理
        private async Task SaveMemoriesToMemoryTableAsync(
            (List<ProcessDetail> details, 
            List<CY> cylinders, 
            List<Operation> operations, List<IO> ioList, List<Models.Timer> timers) prepData)
        {
            if (_memoryService == null)
            {
                MessageBox.Show("MemoryServiceが初期化されていません。", "エラー");
                return;
            }

            var devices = _mnemonicService!.GetMnemonicDevice(SelectedPlc!.Id);
            var timerDevices = _timerService!.GetMnemonicTimerDevice(SelectedPlc!.Id, SelectedCycle!.Id);

            var devicesP = devices.Where(m => m.MnemonicId == (int)KdxDesigner.Models.Define.MnemonicType.Process).ToList();
            var devicesD = devices.Where(m => m.MnemonicId == (int)KdxDesigner.Models.Define.MnemonicType.ProcessDetail).ToList();
            var devicesO = devices.Where(m => m.MnemonicId == (int)KdxDesigner.Models.Define.MnemonicType.Operation).ToList();
            var devicesC = devices.Where(m => m.MnemonicId == (int)KdxDesigner.Models.Define.MnemonicType.CY).ToList();

            MemoryProgressMax = (IsProcessMemory ? devicesP.Count : 0) +
                                (IsDetailMemory ? devicesD.Count : 0) +
                                (IsOperationMemory ? devicesO.Count : 0) +
                                (IsCylinderMemory ? devicesC.Count : 0) +
                                (IsErrorMemory ? devicesC.Count : 0) +
                                (IsTimerMemory ? timerDevices.Count * 2 : 0);
            MemoryProgressValue = 0;

            if (!await ProcessAndSaveMemoryAsync(IsErrorMemory, devicesC, _memoryService.SaveMnemonicMemories, "エラー")) return;

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

        #region Data Migration Commands


        #endregion

    }
}
