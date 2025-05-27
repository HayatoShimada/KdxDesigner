using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Utils.MnemonicCommon;

using System.Diagnostics;

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
            errors = new List<OutputError>();                   // エラーリストの初期化
            var result = new List<LadderCsvRow>();

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

            // StartSensorが設定されている場合は、IOリストからセンサーを取得
            if (detail.Detail.StartSensor != null)
            {
                // ioの取得を共通コンポーネント化すること
                var ioSensor = ioList.FirstOrDefault(io => io.IOName.Contains(detail.Detail.StartSensor));
                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddLD(""));
                    return result; // エラーがあれば空のリストを返す
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
            result.Add(LadderRow.AddOR(label + (deviceNum + 0).ToString()));
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

            return result;

        }

        // 工程まとめを出力するメソッド
        public static List<LadderCsvRow> BuildDetailSummarize(
            MnemonicDeviceWithProcessDetail process,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            errors = new List<OutputError>();                   // エラーリストの初期化
            var result = new List<LadderCsvRow>();

            // L***0 ~ L***9のDeviceリストを取得
            return result;

        }

        // センサON確認
        public static List<LadderCsvRow> BuildDetailSensorON(
            MnemonicDeviceWithProcessDetail process,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            errors = new List<OutputError>();                   // エラーリストの初期化
            var result = new List<LadderCsvRow>();

            // L***0 ~ L***9のDeviceリストを取得
            return result;

        }

        // センサOFF確認
        public static List<LadderCsvRow> BuildDetailSensorOFF(
            MnemonicDeviceWithProcessDetail process,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            errors = new List<OutputError>();                   // エラーリストの初期化
            var result = new List<LadderCsvRow>();

            // L***0 ~ L***9のDeviceリストを取得
            return result;

        }

        // 工程分岐
        public static List<LadderCsvRow> BuildDetailBranch(
            MnemonicDeviceWithProcessDetail process,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            errors = new List<OutputError>();                   // エラーリストの初期化
            var result = new List<LadderCsvRow>();

            // L***0 ~ L***9のDeviceリストを取得
            return result;

        }

        // 工程合流
        public static List<LadderCsvRow> BuildDetailMerge(
            MnemonicDeviceWithProcessDetail process,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            errors = new List<OutputError>();                   // エラーリストの初期化
            var result = new List<LadderCsvRow>();

            // L***0 ~ L***9のDeviceリストを取得
            return result;

        }

        // IL待ち
        public static List<LadderCsvRow> BuildDetailILWait(
            MnemonicDeviceWithProcessDetail process,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            errors = new List<OutputError>();                   // エラーリストの初期化
            var result = new List<LadderCsvRow>();

            // L***0 ~ L***9のDeviceリストを取得
            return result;

        }

        // 工程OFF確認
        public static List<LadderCsvRow> BuildDetailProcessOFF(
            MnemonicDeviceWithProcessDetail process,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            errors = new List<OutputError>();                   // エラーリストの初期化
            var result = new List<LadderCsvRow>();

            // L***0 ~ L***9のDeviceリストを取得
            return result;

        }

        // 期間工程
        public static List<LadderCsvRow> BuildDetailSeason(
            MnemonicDeviceWithProcessDetail process,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            errors = new List<OutputError>();                   // エラーリストの初期化
            var result = new List<LadderCsvRow>();

            // L***0 ~ L***9のDeviceリストを取得
            return result;

        }
    }
}
