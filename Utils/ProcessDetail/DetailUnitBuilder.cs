using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services.Access;
using KdxDesigner.Services.Error;
using KdxDesigner.Services.IOAddress;
using KdxDesigner.Utils.MnemonicCommon;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Utils.ProcessDetail
{
    // ... BuildDetailクラス ...

    /// <summary>
    /// 単一の工程詳細レコードのラダー生成ロジックをカプセル化するクラス。
    /// </summary>
    internal class DetailUnitBuilder
    {
        // --- サービスのフィールド ---
        private readonly MainViewModel _mainViewModel;
        private readonly IIOAddressService _ioAddressService;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IAccessRepository _repository;

        // --- データセットのフィールド ---
        private readonly MnemonicDeviceWithProcessDetail _detail;
        private readonly List<MnemonicDeviceWithProcessDetail> _details;
        private readonly List<MnemonicDeviceWithProcess> _processes;
        private readonly List<MnemonicDeviceWithOperation> _operations;
        private readonly List<MnemonicDeviceWithCylinder> _cylinders;
        private readonly List<IO> _ioList;

        // --- 派生したフィールド ---
        private readonly MnemonicDeviceWithProcess _process;
        private readonly string _label;
        private readonly int _deviceNum;

        /// <summary>
        /// コンストラクタで、ビルドに必要なすべての情報を受け取る
        /// </summary>
        public DetailUnitBuilder(
            // データセット
            MnemonicDeviceWithProcessDetail detail,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            // サービス
            MainViewModel mainViewModel,
            IIOAddressService ioAddressService,
            IErrorAggregator errorAggregator,
            IAccessRepository repository)
        {
            // --- サービス ---
            _mainViewModel = mainViewModel;
            _ioAddressService = ioAddressService;
            _errorAggregator = errorAggregator;
            _repository = repository;

            // --- データセット ---
            _detail = detail;
            _details = details;
            _processes = processes;
            _operations = operations;
            _cylinders = cylinders;
            _ioList = ioList;

            // --- 派生データ (コンストラクタで一度だけ計算) ---
            _label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            _deviceNum = detail.Mnemonic.StartNum;
            _process = _processes.First(p => p.Mnemonic.RecordId == _detail.Detail.ProcessId);
        }

        /// <summary>
        /// 通常工程のビルド
        /// </summary>
        public List<LadderCsvRow> BuildNormal()
        {
            var result = new List<LadderCsvRow>();
            result.Add(CreateStatement());

            var detailFunctions = CreateDetailFunctions();

            // L0 工程開始
            result.AddRange(detailFunctions.L0());

            // L1 操作開始
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_deviceNum + 1).ToString()));
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 1).ToString()));

            // L9 工程完了
            var operationFinish = _operations.FirstOrDefault(o => o.Mnemonic.RecordId == _detail.Detail.OperationId);
            var opFinishNum = operationFinish?.Mnemonic.StartNum ?? 0;
            var opFinishLabel = operationFinish?.Mnemonic.DeviceLabel ?? string.Empty;

            result.Add(LadderRow.AddLD(opFinishLabel + (opFinishNum + 19).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_deviceNum + 9).ToString()));
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 9).ToString()));

            return result;
        }

        /// <summary>
        /// 工程まとめのビルド
        /// </summary>
        /// <returns></returns>
        public List<LadderCsvRow> BuildSummarize()
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement());
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
            result.AddRange(detailFunctions.L0());


            //3行目 M3300
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));

            //4行目この工程の最終の出力番号算出と LDコマンド発行

            result.Add(LadderRow.AddOR(_label + (_deviceNum + 9).ToString()));
            //3行目 M3300後の　L0初期値　AND
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 0).ToString()));

            //3行目 OUT出力
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 9).ToString()));

            return result;

        }

        /// <summary>
        /// センサON確認のビルド
        /// </summary>
        /// <returns></returns>
        public List<LadderCsvRow> BuildSensorON()
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement());
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
            result.AddRange(detailFunctions.L0());


            //3行目　センサー名称からIOリスト参照

            // FinishSensorが設定されている場合は、IOリストからセンサーを取得
            if (_detail.Detail.FinishSensor != null)
            {
                // ioの取得を共通コンポーネント化すること
                var plcId = _mainViewModel.SelectedPlc!.Id;
                var ioSensor = _ioAddressService.GetSingleAddress(
                    _ioList,
                    _detail.Detail.FinishSensor,
                    false,
                    _detail.Detail.DetailName!,
                    _detail.Detail.Id,
                    null);

                if (ioSensor == null)//　万一nullの場合は　空でLD接点入れておく
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                }
                else
                {
                    if (_detail.Detail.FinishSensor.Contains("_"))    // ON工程　→　_の有無問わず　LD接点
                    {
                        result.Add(LadderRow.AddLDI(ioSensor));//恐らく使用されない
                    }
                    else
                    {
                        result.Add(LadderRow.AddLD(ioSensor));

                    }
                }
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));

            }
            else
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            }

            //4行目 
            result.Add(LadderRow.AddOR(_label + (_deviceNum + 1).ToString()));

            //3行目　M3300　の後
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 0).ToString()));

            //3行目 OUT
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 1).ToString()));

            //5行目
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));

            //6行目
            result.Add(LadderRow.AddOR(_label + (_deviceNum + 9).ToString()));

            //5行目
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 9).ToString()));


            // エラーをまとめて返す。
            return result;
        }

        // --- 他のBuild***メソッドも同様にここに移植 ---
        public List<LadderCsvRow> BuildSensorOFF()
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement());
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
            result.AddRange(detailFunctions.L0());


            //3行目　センサー名称からIOリスト参照

            // FinishSensorが設定されている場合は、IOリストからセンサーを取得
            if (_detail.Detail.FinishSensor != null)
            {
                // ioの取得を共通コンポーネント化すること
                var plcId = _mainViewModel.SelectedPlc!.Id;
                var ioSensor = _ioAddressService.GetSingleAddress(
                    _ioList,
                    _detail.Detail.FinishSensor,
                    false,
                    _detail.Detail.DetailName!,
                    _detail.Detail.Id,
                    null);

                if (ioSensor == null)//　万一nullの場合は　空でLD接点入れておく
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));

                }
                else
                {
                    if (_detail.Detail.FinishSensor.Contains("_"))    // OFF工程　→　_の有無問わず　LDI接点
                    {
                        result.Add(LadderRow.AddLDI(ioSensor));//恐らく使用されない
                    }
                    else
                    {
                        result.Add(LadderRow.AddLDI(ioSensor));

                    }
                }
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));

            }
            else
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            }

            //4行目 
            result.Add(LadderRow.AddOR(_label + (_deviceNum + 1).ToString()));

            //3行目　M3300　の後
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 0).ToString()));

            //3行目 OUT
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 1).ToString()));

            //5行目
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));

            //6行目
            result.Add(LadderRow.AddOR(_label + (_deviceNum + 9).ToString()));

            //5行目
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 9).ToString()));

            // エラーをまとめて返す。
            return result;
        }

        public List<LadderCsvRow> BuildBranch()
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement());
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
            result.AddRange(detailFunctions.L0());

            // L1 操作開始
            // StartSensorが設定されている場合は、IOリストからセンサーを取得
            if (_detail.Detail.StartSensor != null)
            {
                var ioSensor = _ioAddressService.GetSingleAddress(
                    _ioList,
                    _detail.Detail.StartSensor,
                    false,
                    _detail.Detail.DetailName!,
                    _detail.Detail.Id,
                    null);

                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                }
                else
                {
                    if (_detail.Detail.StartSensor.Contains("_"))    // Containsではなく、先頭一文字
                    {
                        result.Add(LadderRow.AddLDI(ioSensor));
                    }
                    else
                    {
                        result.Add(LadderRow.AddLD(ioSensor));

                    }
                }

            }
            else
            {
                // StartSensornの設定ナシ
                result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                _errorAggregator.AddError(new OutputError
                {
                    Message = "StartSensor が設定されていません。",
                    RecordName = _detail.Detail.DetailName,
                    MnemonicId = (int)MnemonicType.ProcessDetail,
                    RecordId = _detail.Detail.Id,
                    IsCritical = false
                });
            }

            result.Add(LadderRow.AddANI(_label + (_deviceNum + 2).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_deviceNum + 1).ToString()));
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 1).ToString()));

            // L9 工程完了
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_deviceNum + 9).ToString()));
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 9).ToString()));


            return result;
        }

        public List<LadderCsvRow> BuildMerge()
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            string id = _detail.Detail.Id.ToString();
            if (string.IsNullOrEmpty(_detail.Detail.DetailName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + _detail.Detail.DetailName));
            }

            // L0 の初期値を設定
            var deviceNum = _detail.Mnemonic.StartNum;
            var label = _detail.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessIdからデバイスを取得
            var process = _processes.FirstOrDefault(p => p.Mnemonic.RecordId == _detail.Detail.ProcessId);
            var processDeviceStartNum = process?.Mnemonic.StartNum ?? 0;
            var processDeviceLabel = process?.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessDetailの開始条件
            // この辺の処理がややこしいので共通コンポーネント化すること
            var processDetailStartIds = _detail.Detail.StartIds?.Split(';')
                .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList() ?? new List<int>();
            var processDetailStartDevices = _details
                .Where(d => processDetailStartIds.Contains(d.Mnemonic.RecordId))
                .ToList();

            // L0 工程開始
            // 設定値を使う場合の構文 SettingsManager.Settings.""
            // 設定値の初期値は\Model\AppSettings.csに定義

            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (deviceNum + 0).ToString()));
            result.Add(LadderRow.AddAND(processDeviceLabel + (processDeviceStartNum + 0).ToString()));

            // 初回はLD命令
            var first = true;

            foreach (var d in processDetailStartDevices)
            {
                var row = first
                    ? LadderRow.AddLD(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 9).ToString())
                    : LadderRow.AddOR(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 9).ToString());

                result.Add(row);
                first = false;
            }
            result.Add(LadderRow.AddANB());
            result.Add(LadderRow.AddOUT(label + (deviceNum + 0).ToString()));

            // L9 工程完了
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (deviceNum + 9).ToString()));
            result.Add(LadderRow.AddAND(label + (deviceNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (deviceNum + 9).ToString()));

            // エラーをまとめて返す。

            return result;

        }

        public List<LadderCsvRow> BuildILWait()
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement());
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
            result.AddRange(detailFunctions.L0());

            // L1 操作開始
            if (_detail.Detail.FinishSensor != null)
            {
                // StartSensorが設定されている場合
                result.Add(LadderRow.AddLD(_detail.Detail.FinishSensor));
            }
            else
            {
                // StartSensornの設定ナシ
                result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                detailFunctions.DetailError("FinishSensor が設定されていません。");
            }

            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_deviceNum + 1).ToString()));
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 1).ToString()));

            // L9 工程完了
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_deviceNum + 9).ToString()));
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 9).ToString()));


            return result;
        }

        public List<LadderCsvRow> BuildDetailProcessOFF()
        {
            var result = new List<LadderCsvRow>();
            List<OutputError> localErrors = new();

            // L***0 ~ L***9のDeviceリストを取得

            // エラーをまとめて返す。
            return result;
        }

        public List<LadderCsvRow> BuildSeason()
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement());
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
            result.AddRange(detailFunctions.L0());

            // L1 操作開始
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_deviceNum + 1).ToString()));
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 1).ToString()));

            // L2 操作停止
            // FinishSensorが設定されている場合は、IOリストからセンサーを取得
            if (_detail.Detail.FinishSensor != null)
            {
                // ioの取得を共通コンポーネント化すること
                var ioSensor = _ioAddressService.GetSingleAddress(
                    _ioList,
                    _detail.Detail.FinishSensor,
                    false,
                    _detail.Detail.DetailName!,
                    _detail.Detail.Id,
                    null);

                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                }
                else
                {
                    if (_detail.Detail.FinishSensor.Contains("_"))    // Containsではなく、先頭一文字
                    {
                        result.Add(LadderRow.AddLDI(ioSensor ?? ""));
                    }
                    else
                    {
                        result.Add(LadderRow.AddLD(ioSensor ?? ""));

                    }
                }
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));

            }
            else
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            }

            result.Add(LadderRow.AddOR(_label + (_deviceNum + 2).ToString()));
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 1).ToString()));


            var processDetailFinishDevices = detailFunctions.FinishDevices();
            if (_detail.Detail.FinishSensor != null)
            {
                // FinishSensorが設定されている場合
                // FinishIdsのStartNum+1を出力
                foreach (var d in processDetailFinishDevices)
                {
                    result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 1).ToString()));
                }
            }
            else
            {
                // FinishSensorが設定されていない場合
                // FinishIdsのStartNum+9を出力
                foreach (var d in processDetailFinishDevices)
                {
                    result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 9).ToString()));
                }
            }
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 2).ToString()));

            // L9 工程完了
            // detailのoperationIdからOperationの先頭デバイスを取得
            var operationFinish = _operations.FirstOrDefault(o => o.Mnemonic.RecordId == _detail.Detail.OperationId);
            var operationFinishStartNum = operationFinish?.Mnemonic.StartNum ?? 0;
            var operationFinishDeviceLabel = operationFinish?.Mnemonic.DeviceLabel ?? string.Empty;

            result.Add(LadderRow.AddLD(operationFinishDeviceLabel + (operationFinishStartNum + 19).ToString()));
            result.Add(LadderRow.AddOR(_label + (_deviceNum + 9).ToString()));
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 9).ToString()));

            return result;
        }

        public List<LadderCsvRow> BuildTimerProcess(List<MnemonicTimerDeviceWithDetail> detailTimers)
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement());
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
            result.AddRange(detailFunctions.L0());

            // L1 操作開始
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(_label + (_deviceNum + 1).ToString()));
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 1).ToString()));

            // L2 タイマ開始
            var stopTimer = detailTimers.FirstOrDefault(t => t.Timer.RecordId == _detail.Detail.Id);

            if (stopTimer == null)
            {
                detailFunctions.DetailError("タイマー工程にタイマが設定されていません。");
                return result; // エラーがある場合は、空のリストを返す
            }

            result.Add(LadderRow.AddLD(_label + (_deviceNum + 1).ToString()));
            result.Add(LadderRow.AddANI(_label + (_deviceNum + 2).ToString()));
            result.AddRange(LadderRow.AddTimer(stopTimer.Timer.ProcessTimerDevice, stopTimer.Timer.TimerDevice));
            result.Add(LadderRow.AddLD(stopTimer.Timer.ProcessTimerDevice));
            result.Add(LadderRow.AddOR(_label + (_deviceNum + 2).ToString()));
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 2).ToString()));

            // L9 工程完了
            // detailのoperationIdからOperationの先頭デバイスを取得
            var operationFinish = _operations.FirstOrDefault(o => o.Mnemonic.RecordId == _detail.Detail.OperationId);
            var operationFinishStartNum = operationFinish?.Mnemonic.StartNum ?? 0;
            var operationFinishDeviceLabel = operationFinish?.Mnemonic.DeviceLabel ?? string.Empty;

            result.Add(LadderRow.AddLD(operationFinishDeviceLabel + (operationFinishStartNum + 19).ToString()));
            result.Add(LadderRow.AddOR(_label + (_deviceNum + 9).ToString()));
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 9).ToString()));

            return result;
        }

        public List<LadderCsvRow> BuildTimer(List<MnemonicTimerDeviceWithDetail> detailTimers)
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメントを追加
            result.Add(CreateStatement());
            // L0 工程開始
            var detailFunctions = CreateDetailFunctions();
            result.AddRange(detailFunctions.L0());

            // L9 工程完了
            // detailのoperationIdからOperationの先頭デバイスを取得
            var operationFinish = _operations.FirstOrDefault(o => o.Mnemonic.RecordId == _detail.Detail.OperationId);
            var operationFinishStartNum = operationFinish?.Mnemonic.StartNum ?? 0;
            var operationFinishDeviceLabel = operationFinish?.Mnemonic.DeviceLabel ?? string.Empty;

            result.Add(LadderRow.AddLD(operationFinishDeviceLabel + (operationFinishStartNum + 19).ToString()));
            result.Add(LadderRow.AddOR(_label + (_deviceNum + 9).ToString()));
            result.Add(LadderRow.AddAND(_label + (_deviceNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_deviceNum + 9).ToString()));


            return result;
        }


        #region Private Helper Methods

        /// <summary>
        /// 行間ステートメントを生成する共通処理
        /// </summary>
        private LadderCsvRow CreateStatement()
        {
            string id = _detail.Detail.Id.ToString();
            return LadderRow.AddStatement(id + ":" + _detail.Detail.DetailName);
        }

        /// <summary>
        /// BuildDetailFunctions のインスタンスを生成する共通処理
        /// </summary>
        private BuildDetailFunctions CreateDetailFunctions()
        {
            return new BuildDetailFunctions(
                _detail,
                _process,
                _mainViewModel,
                _ioAddressService,
                _errorAggregator,
                _repository,
                _processes,
                _details,
                _operations,
                _cylinders,
                _ioList
            );
        }

        #endregion
    }
}