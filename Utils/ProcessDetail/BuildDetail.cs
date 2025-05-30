using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Utils.MnemonicCommon;

using System.Diagnostics;
using Windows.ApplicationModel.Contacts;

// ProcessDetail（工程プログラム）のニモニック配列を返すコード群

namespace KdxDesigner.Utils.ProcessDetail
{
    internal class BuildDetail
    {
        // 通常工程を出力するメソッド
        public static List<LadderCsvRow> BuildDetailNormal(
            MnemonicDeviceWithProcessDetail detail,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            var result = new List<LadderCsvRow>();
            errors = new List<OutputError>();                   // エラーリストの初期化
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
                .Select(n => n.Value)
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
                var ioSensor = ioList.FirstOrDefault(io => io.IOName.Contains(detail.Detail.StartSensor));
                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddLD(""));
                    localErrors.Add(new OutputError
                    {
                        Message = $"StartSensor '{detail.Detail.StartSensor}' が見つかりませんでした。",
                        DetailName = detail.Detail.ProcessName,
                        MnemonicId = (int)MnemonicType.ProcessDetail,
                        ProcessId = detail.Detail.Id
                    });
                }
                else
                {
                    if (detail.Detail.StartSensor.Contains("_"))    // Containsではなく、先頭一文字
                    {
                        result.Add(LadderRow.AddLDI(ioSensor.Address ?? ""));
                    }
                    else
                    {
                        result.Add(LadderRow.AddLD(ioSensor.Address ?? ""));

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

            result.Add(LadderRow.AddLD(operationFinishDeviceLabel +　(operationFinishStartNum + 49).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (deviceNum + 9).ToString()));
            result.Add(LadderRow.AddAND(label + (deviceNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(label + (deviceNum + 9).ToString()));

            // エラーをまとめて返す。
            errors.AddRange(localErrors);

            return result;

        }

        // 工程まとめを出力するメソッド
        public static List<LadderCsvRow> BuildDetailSummarize(
            MnemonicDeviceWithProcessDetail detail,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            errors = new List<OutputError>();                   // エラーリストの初期化
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
                .Select(n => n.Value)
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
            errors.AddRange(localErrors);
            return result;

        }

        // センサON確認
        public static List<LadderCsvRow> BuildDetailSensorON(
            MnemonicDeviceWithProcessDetail process,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            errors = new List<OutputError>();                   // エラーリストの初期化
            var result = new List<LadderCsvRow>();
            List<OutputError> localErrors = new();

            // L***0 ~ L***9のDeviceリストを取得

            // エラーをまとめて返す。
            errors.AddRange(localErrors);
            return result;

        }

        // センサOFF確認
        public static List<LadderCsvRow> BuildDetailSensorOFF(
            MnemonicDeviceWithProcessDetail process,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            errors = new List<OutputError>();                   // エラーリストの初期化
            var result = new List<LadderCsvRow>();
            List<OutputError> localErrors = new();

            // L***0 ~ L***9のDeviceリストを取得

            // エラーをまとめて返す。
            errors.AddRange(localErrors);
            return result;

        }

        // 工程分岐
        public static List<LadderCsvRow> BuildDetailBranch(
            MnemonicDeviceWithProcessDetail detail,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            var result = new List<LadderCsvRow>();
            errors = new List<OutputError>();                   // エラーリストの初期化
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
                .Select(n => n.Value)
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
                // ioの取得を共通コンポーネント化すること
                var ioSensor = ioList.FirstOrDefault(io => io.IOName.Contains(detail.Detail.StartSensor));

                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddLD(""));
                    localErrors.Add(new OutputError
                    {
                        Message = $"StartSensor '{detail.Detail.StartSensor}' が見つかりませんでした。",
                        DetailName = detail.Detail.ProcessName,
                        MnemonicId = (int)MnemonicType.ProcessDetail,
                        ProcessId = detail.Detail.Id
                    });
                }
                else
                {
                    if (detail.Detail.StartSensor.Contains("_"))    // Containsではなく、先頭一文字
                    {
                        result.Add(LadderRow.AddLDI(ioSensor.Address ?? ""));
                    }
                    else
                    {
                        result.Add(LadderRow.AddLD(ioSensor.Address ?? ""));

                    }
                }
                //result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));

            }
            else
            {
                // StartSensornの設定ナシ
                result.Add(LadderRow.AddLD(""));
                localErrors.Add(new OutputError
                {
                    Message = "StartSensor が設定されていません。",
                    DetailName = detail.Detail.ProcessName,
                    MnemonicId = (int)MnemonicType.ProcessDetail,
                    ProcessId = detail.Detail.Id
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

            // エラーをまとめて返す。
            errors.AddRange(localErrors);

            return result;

        }

        // 工程合流
        public static List<LadderCsvRow> BuildDetailMerge(
            MnemonicDeviceWithProcessDetail detail,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            var result = new List<LadderCsvRow>();
            errors = new List<OutputError>();                   // エラーリストの初期化
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
                .Select(n => n.Value)
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
                    ?LadderRow.AddLD(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 9).ToString())
                    :LadderRow.AddOR(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 9).ToString());

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
            errors.AddRange(localErrors);

            return result;

        }

        // IL待ち
        public static List<LadderCsvRow> BuildDetailILWait(
            MnemonicDeviceWithProcessDetail process,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            errors = new List<OutputError>();                   // エラーリストの初期化
            var result = new List<LadderCsvRow>();
            List<OutputError> localErrors = new();

            // L***0 ~ L***9のDeviceリストを取得

            // エラーをまとめて返す。
            errors.AddRange(localErrors);
            return result;
        }

        // 工程OFF確認
        public static List<LadderCsvRow> BuildDetailProcessOFF(
            MnemonicDeviceWithProcessDetail process,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            errors = new List<OutputError>();                   // エラーリストの初期化
            var result = new List<LadderCsvRow>();
            List<OutputError> localErrors = new();

            // L***0 ~ L***9のDeviceリストを取得

            // エラーをまとめて返す。
            errors.AddRange(localErrors);
            return result;

        }

        // 期間工程
        public static List<LadderCsvRow> BuildDetailSeason(
            MnemonicDeviceWithProcessDetail process,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            errors = new List<OutputError>();                   // エラーリストの初期化
            var result = new List<LadderCsvRow>();
            List<OutputError> localErrors = new();

            // L***0 ~ L***9のDeviceリストを取得

            // エラーをまとめて返す。
            errors.AddRange(localErrors);
            return result;

        }
    }
}
