using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services;
using KdxDesigner.Services.Error;
using KdxDesigner.Utils.MnemonicCommon;
using KdxDesigner.ViewModels;

using Windows.AI.MachineLearning;

// ProcessDetail（工程プログラム）のニモニック配列を返すコード群

namespace KdxDesigner.Utils.ProcessDetail
{
    internal class BuildDetail
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;

        public BuildDetail(MainViewModel mainViewModel,IIOAddressService ioAddressService, IErrorAggregator errorAggregator)
        {
            _mainViewModel = mainViewModel; // MainViewModelのインスタンスを取得
            _ioAddressService = ioAddressService; // IOアドレスサービスのインスタンスを取得
            _errorAggregator = errorAggregator;   // エラーアグリゲーターのインスタンスを取得
        }

        // 通常工程を出力するメソッド
        public List<LadderCsvRow> BuildDetailNormal(
            MnemonicDeviceWithProcessDetail detail,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList)
        {
            var result = new List<LadderCsvRow>();
            List<OutputError> localErrors = new();

            // 行間ステートメントを追加
            string id = detail.Detail.Id.ToString();
            if (string.IsNullOrEmpty(detail.Detail.ProcessName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + detail.Detail.ProcessName));
            }

            // L0 の初期値を設定
            var deviceNum = detail.Mnemonic.StartNum;
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessIdからデバイスを取得　流用する
            var process = processes.FirstOrDefault(p => p.Mnemonic.RecordId == detail.Detail.ProcessId);
            var processDeviceStartNum = process?.Mnemonic.StartNum ?? 0;
            var processDeviceLabel = process?.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessDetailの開始条件
            // この辺の処理がややこしいので共通コンポーネント化すること
            var processDetailStartIds = detail.Detail.StartIds?.Split(';')
                .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList() ?? new List<int>();
            var processDetailStartDevices = details
                .Where(d => processDetailStartIds.Contains(d.Mnemonic.RecordId))
                .ToList();

            // L0 工程開始
            // 設定値を使う場合の構文 SettingsManager.Settings.""
            // 設定値の初期値は\Model\AppSettings.csに定義

            // StartSensorが設定されている場合は、IOリストからセンサーを取得
            if (detail.Detail.StartSensor != null)
            {
                // ioの取得を共通コンポーネント化すること
                var plcId = _mainViewModel.SelectedPlc!.Id;
                var ioSensor = _ioAddressService.GetSingleAddress(ioList, detail.Detail.StartSensor, plcId);

                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                }
                else
                {
                    if (detail.Detail.StartSensor.Contains("_"))    // Containsではなく、先頭一文字
                    {
                        result.Add(LadderRow.AddLDI(ioSensor));
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
            //
            result.Add(LadderRow.AddOR(label + (deviceNum + 0).ToString()));
            //L3106 のところ　流用する
            result.Add(LadderRow.AddAND(processDeviceLabel + (processDeviceStartNum + 0).ToString()));

            foreach (var d in processDetailStartDevices)
            {
                result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 9).ToString()));
            }
            result.Add(LadderRow.AddOUT(label + (deviceNum + 0).ToString()));


            // L1 操作開始
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (deviceNum + 1).ToString()));
            result.Add(LadderRow.AddAND(label + (deviceNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (deviceNum + 1).ToString()));

            // L9 工程完了
            // detailのoperationIdからOperationの先頭デバイスを取得
            var operationFinish = operations.FirstOrDefault(o => o.Mnemonic.RecordId == detail.Detail.OperationId);
            var operationFinishStartNum = operationFinish?.Mnemonic.StartNum ?? 0;
            var operationFinishDeviceLabel = operationFinish?.Mnemonic.DeviceLabel ?? string.Empty;

            result.Add(LadderRow.AddLD(operationFinishDeviceLabel + (operationFinishStartNum + 49).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (deviceNum + 9).ToString()));
            result.Add(LadderRow.AddAND(label + (deviceNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(label + (deviceNum + 9).ToString()));

            // エラーをまとめて返す。

            return result;

        }

        // 工程まとめを出力するメソッド
        public List<LadderCsvRow> BuildDetailSummarize(
            MnemonicDeviceWithProcessDetail detail,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList)
        {
            var result = new List<LadderCsvRow>();
            List<OutputError> localErrors = new();

            // 行間ステートメントを追加
            string id = detail.Detail.Id.ToString();
            if (string.IsNullOrEmpty(detail.Detail.ProcessName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + detail.Detail.ProcessName));
            }

            // L0 の初期値を設定　→　資料画像のL4010の事　この後末尾番号として　+9する　+0が開始で　+9が終了
            var deviceNum = detail.Mnemonic.StartNum;
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessIdからデバイスを取得
            var process = processes.FirstOrDefault(p => p.Mnemonic.RecordId == detail.Detail.ProcessId);
            var processDeviceStartNum = process?.Mnemonic.StartNum ?? 0;
            var processDeviceLabel = process?.Mnemonic.DeviceLabel ?? string.Empty;// 

            // ProcessDetailの開始条件
            // この辺の処理がややこしいので共通コンポーネント化すること
            var processDetailStartIds = detail.Detail.StartIds?.Split(';')
                .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList() ?? new List<int>();
            var processDetailStartDevices = details
                .Where(d => processDetailStartIds.Contains(d.Mnemonic.RecordId))
                .ToList();



            // L***0 ~ L***9のDeviceリストを取得

            //1行目
            //　LD Ｍ3300　　一時停止 工程まとめでは共通　
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            //2行目
            //　OR　L****　 プロセス詳細開始条件　→　アウトコイルと同じ
            result.Add(LadderRow.AddOR(label + (deviceNum + 0).ToString()));
            //1行目
            //L3106 のところ　流用する
            result.Add(LadderRow.AddAND(processDeviceLabel + (processDeviceStartNum + 0).ToString()));


            //1行目　他工程の完了確認接点 
            foreach (var d in processDetailStartDevices)
            {
                result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 9).ToString()));
            }
            //1行目　OUTコイル接点　→　2行目の先頭接点と同じ　
            result.Add(LadderRow.AddOUT(label + (deviceNum + 0).ToString()));


            //3行目 M3300
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));

            //4行目この工程の最終の出力番号算出と LDコマンド発行

            result.Add(LadderRow.AddOR(label + (deviceNum + 9).ToString()));
            //3行目 M3300後の　L0初期値　AND
            result.Add(LadderRow.AddAND(label + (deviceNum + 0).ToString()));


            //3行目 OUT出力
            result.Add(LadderRow.AddOUT(label + (deviceNum + 9).ToString()));



            // エラーをまとめて返す。
            return result;

        }

        // センサON確認
        public List<LadderCsvRow> BuildDetailSensorON(
            MnemonicDeviceWithProcessDetail detail,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList)
        {
            var result = new List<LadderCsvRow>();
            List<OutputError> localErrors = new();

            // L***0 ~ L***9のDeviceリストを取得
            // 行間ステートメントを追加
            string id = detail.Detail.Id.ToString();
            if (string.IsNullOrEmpty(detail.Detail.ProcessName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + detail.Detail.ProcessName));
            }

            // L0 の初期値を設定　→　資料画像のL4010の事　この後末尾番号として　+9する　+0が開始で　+9が終了
            var deviceNum = detail.Mnemonic.StartNum;
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessIdからデバイスを取得
            var process = processes.FirstOrDefault(p => p.Mnemonic.RecordId == detail.Detail.ProcessId);
            var processDeviceStartNum = process?.Mnemonic.StartNum ?? 0;
            var processDeviceLabel = process?.Mnemonic.DeviceLabel ?? string.Empty;// 

            // ProcessDetailの開始条件
            // この辺の処理がややこしいので共通コンポーネント化すること
            var processDetailStartIds = detail.Detail.StartIds?.Split(';')
                .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList() ?? new List<int>();
            var processDetailStartDevices = details
                .Where(d => processDetailStartIds.Contains(d.Mnemonic.RecordId))
                .ToList();

            //1行目
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            //2行目
            //　OR　L****　 プロセス詳細開始条件　→　アウトコイルと同じ
            result.Add(LadderRow.AddOR(label + (deviceNum + 0).ToString()));
            //1行目
            //L3106 のところ　流用する
            result.Add(LadderRow.AddAND(processDeviceLabel + (processDeviceStartNum + 0).ToString()));

            //1行目　他工程の完了確認接点 
            foreach (var d in processDetailStartDevices)
            {
                result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 9).ToString()));
            }

            //1行目　OUTコイル接点　→　2行目の先頭接点と同じ　
            result.Add(LadderRow.AddOUT(label + (deviceNum + 0).ToString()));


            //3行目　センサー名称からIOリスト参照

            // FinishSensorが設定されている場合は、IOリストからセンサーを取得
            if (detail.Detail.FinishSensor != null)
            {
                // ioの取得を共通コンポーネント化すること
                var plcId = _mainViewModel.SelectedPlc!.Id;
                var ioSensor = _ioAddressService.GetSingleAddress(ioList, detail.Detail.FinishSensor, plcId);

                if (ioSensor == null)//　万一nullの場合は　空でLD接点入れておく
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                }
                else
                {
                    if (detail.Detail.FinishSensor.Contains("_"))    // ON工程　→　_の有無問わず　LD接点
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
            result.Add(LadderRow.AddOR(label + (deviceNum + 1).ToString()));

            //3行目　M3300　の後
            result.Add(LadderRow.AddAND(label + (deviceNum + 0).ToString()));

            //3行目 OUT
            result.Add(LadderRow.AddOUT(label + (deviceNum + 1).ToString()));

            //5行目
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));

            //6行目
            result.Add(LadderRow.AddOR(label + (deviceNum + 9).ToString()));

            //5行目
            result.Add(LadderRow.AddAND(label + (deviceNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(label + (deviceNum + 9).ToString()));


            // エラーをまとめて返す。
            return result;

        }

        // センサOFF確認
        public List<LadderCsvRow> BuildDetailSensorOFF(
            MnemonicDeviceWithProcessDetail detail,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList)
        {
            var result = new List<LadderCsvRow>();
            List<OutputError> localErrors = new();

            // L***0 ~ L***9のDeviceリストを取得

            // L***0 ~ L***9のDeviceリストを取得
            // 行間ステートメントを追加
            string id = detail.Detail.Id.ToString();
            if (string.IsNullOrEmpty(detail.Detail.ProcessName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + detail.Detail.ProcessName));
            }

            // L0 の初期値を設定　→　資料画像のL4010の事　この後末尾番号として　+9する　+0が開始で　+9が終了
            var deviceNum = detail.Mnemonic.StartNum;
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessIdからデバイスを取得
            var process = processes.FirstOrDefault(p => p.Mnemonic.RecordId == detail.Detail.ProcessId);
            var processDeviceStartNum = process?.Mnemonic.StartNum ?? 0;
            var processDeviceLabel = process?.Mnemonic.DeviceLabel ?? string.Empty;// 

            // ProcessDetailの開始条件
            // この辺の処理がややこしいので共通コンポーネント化すること
            var processDetailStartIds = detail.Detail.StartIds?.Split(';')
                .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList() ?? new List<int>();
            var processDetailStartDevices = details
                .Where(d => processDetailStartIds.Contains(d.Mnemonic.RecordId))
                .ToList();

            //1行目
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            //2行目
            //　OR　L****　 プロセス詳細開始条件　→　アウトコイルと同じ
            result.Add(LadderRow.AddOR(label + (deviceNum + 0).ToString()));
            //1行目
            //L3106 のところ　流用する
            result.Add(LadderRow.AddAND(processDeviceLabel + (processDeviceStartNum + 0).ToString()));

            //1行目　他工程の完了確認接点 
            foreach (var d in processDetailStartDevices)
            {
                result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 9).ToString()));
            }

            //1行目　OUTコイル接点　→　2行目の先頭接点と同じ　
            result.Add(LadderRow.AddOUT(label + (deviceNum + 0).ToString()));


            //3行目　センサー名称からIOリスト参照

            // FinishSensorが設定されている場合は、IOリストからセンサーを取得
            if (detail.Detail.FinishSensor != null)
            {
                // ioの取得を共通コンポーネント化すること
                var plcId = _mainViewModel.SelectedPlc!.Id;
                var ioSensor = _ioAddressService.GetSingleAddress(ioList, detail.Detail.FinishSensor, plcId);

                if (ioSensor == null)//　万一nullの場合は　空でLD接点入れておく
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));

                }
                else
                {
                    if (detail.Detail.FinishSensor.Contains("_"))    // OFF工程　→　_の有無問わず　LDI接点
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
            result.Add(LadderRow.AddOR(label + (deviceNum + 1).ToString()));

            //3行目　M3300　の後
            result.Add(LadderRow.AddAND(label + (deviceNum + 0).ToString()));

            //3行目 OUT
            result.Add(LadderRow.AddOUT(label + (deviceNum + 1).ToString()));

            //5行目
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));

            //6行目
            result.Add(LadderRow.AddOR(label + (deviceNum + 9).ToString()));

            //5行目
            result.Add(LadderRow.AddAND(label + (deviceNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(label + (deviceNum + 9).ToString()));



            // エラーをまとめて返す。
            return result;

        }

        // 工程分岐
        public List<LadderCsvRow> BuildDetailBranch(
            MnemonicDeviceWithProcessDetail detail,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList)
        {
            var result = new List<LadderCsvRow>();
            List<OutputError> localErrors = new();

            // 行間ステートメントを追加
            string id = detail.Detail.Id.ToString();
            if (string.IsNullOrEmpty(detail.Detail.ProcessName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + detail.Detail.ProcessName));
            }

            // L0 の初期値を設定
            var deviceNum = detail.Mnemonic.StartNum;
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessIdからデバイスを取得
            var process = processes.FirstOrDefault(p => p.Mnemonic.RecordId == detail.Detail.ProcessId);
            var processDeviceStartNum = process?.Mnemonic.StartNum ?? 0;
            var processDeviceLabel = process?.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessDetailの開始条件
            // この辺の処理がややこしいので共通コンポーネント化すること
            var processDetailStartIds = detail.Detail.StartIds?.Split(';')
                .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList() ?? new List<int>();
            var processDetailStartDevices = details
                .Where(d => processDetailStartIds.Contains(d.Mnemonic.RecordId))
                .ToList();

            // L0 工程開始
            // 設定値を使う場合の構文 SettingsManager.Settings.""
            // 設定値の初期値は\Model\AppSettings.csに定義

            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (deviceNum + 0).ToString()));
            result.Add(LadderRow.AddAND(processDeviceLabel + (processDeviceStartNum + 0).ToString()));

            foreach (var d in processDetailStartDevices)
            {
                result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 9).ToString()));
            }
            result.Add(LadderRow.AddOUT(label + (deviceNum + 0).ToString()));

            // L1 操作開始
            // StartSensorが設定されている場合は、IOリストからセンサーを取得
            if (detail.Detail.StartSensor != null)
            {
                var plcId = _mainViewModel.SelectedPlc!.Id;
                var ioSensor = _ioAddressService.GetSingleAddress(ioList, detail.Detail.StartSensor, plcId);

                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                }
                else
                {
                    if (detail.Detail.StartSensor.Contains("_"))    // Containsではなく、先頭一文字
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
                localErrors.Add(new OutputError
                {
                    Message = "StartSensor が設定されていません。",
                    RecordName = detail.Detail.ProcessName,
                    MnemonicId = (int)MnemonicType.ProcessDetail,
                    RecordId = detail.Detail.Id,
                    IsCritical = false
                });
            }

            result.Add(LadderRow.AddANI(label + (deviceNum + 2).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (deviceNum + 1).ToString()));
            result.Add(LadderRow.AddAND(label + (deviceNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (deviceNum + 1).ToString()));

            // L9 工程完了
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (deviceNum + 9).ToString()));
            result.Add(LadderRow.AddAND(label + (deviceNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(label + (deviceNum + 9).ToString()));


            return result;

        }

        // 工程合流
        public List<LadderCsvRow> BuildDetailMerge(
            MnemonicDeviceWithProcessDetail detail,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList)
        {
            var result = new List<LadderCsvRow>();
            List<OutputError> localErrors = new();

            // 行間ステートメントを追加
            string id = detail.Detail.Id.ToString();
            if (string.IsNullOrEmpty(detail.Detail.ProcessName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + detail.Detail.ProcessName));
            }

            // L0 の初期値を設定
            var deviceNum = detail.Mnemonic.StartNum;
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessIdからデバイスを取得
            var process = processes.FirstOrDefault(p => p.Mnemonic.RecordId == detail.Detail.ProcessId);
            var processDeviceStartNum = process?.Mnemonic.StartNum ?? 0;
            var processDeviceLabel = process?.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessDetailの開始条件
            // この辺の処理がややこしいので共通コンポーネント化すること
            var processDetailStartIds = detail.Detail.StartIds?.Split(';')
                .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList() ?? new List<int>();
            var processDetailStartDevices = details
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

        // IL待ち
        public List<LadderCsvRow> BuildDetailILWait(
            MnemonicDeviceWithProcessDetail detail,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList)
        {
            var result = new List<LadderCsvRow>();
            List<OutputError> localErrors = new();

            // 行間ステートメントを追加
            string id = detail.Detail.Id.ToString();
            if (string.IsNullOrEmpty(detail.Detail.ProcessName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + detail.Detail.ProcessName));
            }

            // L0 の初期値を設定
            var deviceNum = detail.Mnemonic.StartNum;
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessIdからデバイスを取得
            var process = processes.FirstOrDefault(p => p.Mnemonic.RecordId == detail.Detail.ProcessId);
            var processDeviceStartNum = process?.Mnemonic.StartNum ?? 0;
            var processDeviceLabel = process?.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessDetailの開始条件
            // この辺の処理がややこしいので共通コンポーネント化すること
            var processDetailStartIds = detail.Detail.StartIds?.Split(';')
                .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList() ?? new List<int>();
            var processDetailStartDevices = details
                .Where(d => processDetailStartIds.Contains(d.Mnemonic.RecordId))
                .ToList();

            // L0 工程開始
            // 設定値を使う場合の構文 SettingsManager.Settings.""
            // 設定値の初期値は\Model\AppSettings.csに定義

            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (deviceNum + 0).ToString()));
            result.Add(LadderRow.AddAND(processDeviceLabel + (processDeviceStartNum + 1).ToString()));

            foreach (var d in processDetailStartDevices)
            {
                result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 9).ToString()));
            }
            result.Add(LadderRow.AddOUT(label + (deviceNum + 0).ToString()));

            // L1 操作開始
            if (detail.Detail.StartSensor != null)
            {
                // StartSensorが設定されている場合
                result.Add(LadderRow.AddLD(detail.Detail.StartSensor));
            }
            else
            {
                // StartSensornの設定ナシ
                result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                localErrors.Add(new OutputError
                {
                    Message = "StartSensor が設定されていません。",
                    RecordName = detail.Detail.ProcessName,
                    MnemonicId = (int)MnemonicType.ProcessDetail,
                    RecordId = detail.Detail.Id,
                    IsCritical = false
                });
            }

            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (deviceNum + 1).ToString()));
            result.Add(LadderRow.AddAND(label + (deviceNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (deviceNum + 1).ToString()));

            // L9 工程完了
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (deviceNum + 9).ToString()));
            result.Add(LadderRow.AddAND(label + (deviceNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(label + (deviceNum + 9).ToString()));


            return result;
        }

        // 工程OFF確認
        public List<LadderCsvRow> BuildDetailProcessOFF(
            MnemonicDeviceWithProcessDetail process,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList)
        {
            var result = new List<LadderCsvRow>();
            List<OutputError> localErrors = new();

            // L***0 ~ L***9のDeviceリストを取得

            // エラーをまとめて返す。
            return result;

        }

        // 期間工程
        public List<LadderCsvRow> BuildDetailSeason(
            MnemonicDeviceWithProcessDetail detail,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList)
        {
            var result = new List<LadderCsvRow>();
            List<OutputError> localErrors = new();

            // 行間ステートメントを追加
            string id = detail.Detail.Id.ToString();
            if (string.IsNullOrEmpty(detail.Detail.ProcessName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + detail.Detail.ProcessName));
            }

            // L0 の初期値を設定
            var deviceNum = detail.Mnemonic.StartNum;
            var label = detail.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessIdからデバイスを取得
            var process = processes.FirstOrDefault(p => p.Mnemonic.RecordId == detail.Detail.ProcessId);
            var processDeviceStartNum = process?.Mnemonic.StartNum ?? 0;
            var processDeviceLabel = process?.Mnemonic.DeviceLabel ?? string.Empty;

            // ProcessDetailの開始条件
            // この辺の処理がややこしいので共通コンポーネント化すること
            var processDetailStartIds = detail.Detail.StartIds?.Split(';')
                .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList() ?? new List<int>();
            var processDetailStartDevices = details
                .Where(d => processDetailStartIds.Contains(d.Mnemonic.RecordId))
                .ToList();

            // ProcessDetailの終了条件
            var processDetailFinishIds = detail.Detail.FinishIds?.Split(';')
                .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList() ?? new List<int>();
            var processDetailFinishDevices = details
                .Where(d => processDetailFinishIds.Contains(d.Mnemonic.RecordId))
                .ToList();


            // L0 工程開始
            // 設定値を使う場合の構文 SettingsManager.Settings.""
            // 設定値の初期値は\Model\AppSettings.csに定義

            // StartSensorが設定されている場合は、IOリストからセンサーを取得
            if (detail.Detail.StartSensor != null)
            {
                // ioの取得を共通コンポーネント化すること
                var plcId = _mainViewModel.SelectedPlc!.Id;
                var ioSensor = _ioAddressService.GetSingleAddress(ioList, detail.Detail.StartSensor, plcId);

                if (ioSensor == null)
                {
                    // StartSensornの設定ナシ
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                }
                else
                {
                    if (detail.Detail.StartSensor.Contains("_"))    // Containsではなく、先頭一文字
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

            result.Add(LadderRow.AddOR(label + (deviceNum + 0).ToString()));
            result.Add(LadderRow.AddAND(processDeviceLabel + (processDeviceStartNum + 1).ToString()));

            foreach (var d in processDetailStartDevices)
            {
                result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 1).ToString()));
            }
            result.Add(LadderRow.AddOUT(label + (deviceNum + 0).ToString()));

            // L1 操作開始
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (deviceNum + 1).ToString()));
            result.Add(LadderRow.AddAND(label + (deviceNum + 0).ToString()));
            result.Add(LadderRow.AddOUT(label + (deviceNum + 1).ToString()));

            // L2 操作停止
            // FinishSensorが設定されている場合は、IOリストからセンサーを取得
            if (detail.Detail.FinishSensor != null)
            {
                // ioの取得を共通コンポーネント化すること
                var plcId = _mainViewModel.SelectedPlc!.Id;
                var ioSensor = _ioAddressService.GetSingleAddress(ioList, detail.Detail.FinishSensor, plcId);

                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                }
                else
                {
                    if (detail.Detail.FinishSensor.Contains("_"))    // Containsではなく、先頭一文字
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

            result.Add(LadderRow.AddOR(label + (deviceNum + 2).ToString()));
            result.Add(LadderRow.AddAND(label + (deviceNum + 1).ToString()));

            if (detail.Detail.FinishSensor != null)
            {
                // FinishSensorが設定されている場合
                foreach (var d in processDetailStartDevices)
                {
                    result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 1).ToString()));
                }
            }
            else
            {
                // FinishSensorが設定されていない場合
                // とりあえずFinishIdsのStartNum+9を出力
                foreach (var d in processDetailFinishDevices)
                {
                    result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 9).ToString()));
                }
            }
            result.Add(LadderRow.AddOUT(label + (deviceNum + 2).ToString()));

            // L9 工程完了
            // detailのoperationIdからOperationの先頭デバイスを取得
            var operationFinish = operations.FirstOrDefault(o => o.Mnemonic.RecordId == detail.Detail.OperationId);
            var operationFinishStartNum = operationFinish?.Mnemonic.StartNum ?? 0;
            var operationFinishDeviceLabel = operationFinish?.Mnemonic.DeviceLabel ?? string.Empty;

            result.Add(LadderRow.AddLD(operationFinishDeviceLabel + (operationFinishStartNum + 19).ToString()));
            result.Add(LadderRow.AddOR(label + (deviceNum + 9).ToString()));
            result.Add(LadderRow.AddAND(label + (deviceNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(label + (deviceNum + 9).ToString()));


            return result;

        }
    }
}
