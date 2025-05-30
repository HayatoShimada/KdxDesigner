using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Utils.MnemonicCommon;

using System.Diagnostics;

namespace KdxDesigner.Utils.Operation

{
    internal class BuildOperationSingle
    {

        public static List<LadderCsvRow> Retention(
            MnemonicDeviceWithOperation operation,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<MnemonicTimerDeviceWithOperation> timers,
            List<IO> ioList,
            out List<OutputError> errors,
            int plcId)
        {
            // ここに単一工程の処理を実装
            errors = new List<OutputError>(); // エラーリストの初期化
            var result = new List<LadderCsvRow>(); // 生成されるLadderCsvRowのリスト
            List<OutputError> localErrors = new();

            var label = operation.Mnemonic.DeviceLabel; // ラベルの取得
            var outNum = operation.Mnemonic.StartNum; // スタート番号の取得

            var operationTimers = timers.Where(t => t.Operation.Id == operation.Operation.Id).ToList();

            // 実際の処理ロジックをここに追加

            // 行間ステートメント
            string id = operation.Operation.Id.ToString();
            if (string.IsNullOrEmpty(operation.Operation.OperationName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + operation.Operation.OperationName));
            }

            // OperationIdが一致する工程詳細のフィルタリング
            // M0
            var detailList = details.Where(d => d.Detail.OperationId == operation.Operation.Id).ToList();

            if (detailList.Count == 0)
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                result.Add(LadderRow.AddOUT(label + (outNum + 0).ToString()));
            }
            else
            {
                bool notFirst = false; // 最初の工程詳細かどうかを示すフラグ
                foreach (var detail in detailList)
                {
                    var detailLabel = detail.Mnemonic.DeviceLabel; // 工程詳細のラベル取得
                    var detailOutNum = detail.Mnemonic.StartNum; // 工程詳細のラベル取得

                    result.Add(LadderRow.AddLD(label + (detailOutNum + 1).ToString()));
                    result.Add(LadderRow.AddANI(label + (detailOutNum + 9).ToString()));
                    if (notFirst) result.Add(LadderRow.AddORB());
                    notFirst = true;
                }
                result.Add(LadderRow.AddOUT(label + (outNum + 0).ToString()));
            }

            // M2
            result.Add(LadderRow.AddLD(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddSET(label + (outNum + 2).ToString()));

            // M5
            result.Add(LadderRow.AddLD(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddLD(label + (outNum + 2).ToString()));
            result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddORB());

            result.Add(LadderRow.AddOR(label + (outNum + 5).ToString()));
            result.Add(LadderRow.AddANI(label + (outNum + 19).ToString()));
            result.Add(LadderRow.AddANI(SettingsManager.Settings.SoftResetSignal)); // ソフトリセット信号を追加
            result.Add(LadderRow.AddANI(label + (outNum + 4).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 5).ToString()));

            // 開始待ちタイマがある場合
            var operationTimerWait = operationTimers.FirstOrDefault(t => t.Timer.TimerCategoryId == 1);
            if (operationTimerWait != null)
            {
                result.Add(LadderRow.AddLD(label + (outNum + 5).ToString()));
                result.Add(LadderRow.AddANI(label + (outNum + 6).ToString()));
                result.AddRange(LadderRow.AddTimer(
                    operationTimerWait.Timer.ProcessTimerDevice ?? "",
                    operationTimerWait.Timer.TimerDevice ?? ""
                    ));
            }

            // M6
            // 開始待ちタイマがあるかどうかで分岐
            if (operationTimerWait != null)
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
                result.Add(LadderRow.AddAND(operationTimerWait.Timer.ProcessTimerDevice ?? ""));
            }
            else
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
            }
            result.Add(LadderRow.AddOR(label + (outNum + 6).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 5).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 6).ToString()));

            // M7
            // Start信号がある場合のみ回路を生成
            if (operation.Operation.Start != null)
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
                // ioの取得を共通コンポーネント化すること
                var ioSensor = IOAddress.FindByIOText(ioList, operation.Operation.Start, plcId, out localErrors);

                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddAND(SettingsManager.Settings.AlwaysON));
                    localErrors.Add(new OutputError
                    {
                        Message = $"StartSensor '{operation.Operation.Start}' が見つかりませんでした。",
                        DetailName = operation.Operation.OperationName,
                        MnemonicId = (int)MnemonicType.Operation,
                        ProcessId = operation.Operation.Id
                    });
                }
                else
                {
                    if (operation.Operation.Start.Contains("_"))    // Containsではなく、先頭一文字
                    {
                        result.Add(LadderRow.AddAND(ioSensor ?? ""));
                    }
                    else
                    {
                        result.Add(LadderRow.AddANI(ioSensor ?? ""));

                    }
                }
                result.Add(LadderRow.AddOR(label + (outNum + 6).ToString()));
                result.Add(LadderRow.AddAND(label + (outNum + 5).ToString()));
                result.Add(LadderRow.AddOUT(label + (outNum + 7).ToString()));
            }

            // M17
            if (operation.Operation.Finish != null)
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
                // ioの取得を共通コンポーネント化すること
                var ioSensor = IOAddress.FindByIOText(ioList, operation.Operation.Finish, plcId, out localErrors);

                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddAND(SettingsManager.Settings.AlwaysON));
                    localErrors.Add(new OutputError
                    {
                        Message = $"FinishSensor '{operation.Operation.Finish}' が見つかりませんでした。",
                        DetailName = operation.Operation.OperationName,
                        MnemonicId = (int)MnemonicType.Operation,
                        ProcessId = operation.Operation.Id
                    });
                }
                else
                {
                    if (operation.Operation.Finish.Contains("_"))    // Containsではなく、先頭一文字
                    {
                        result.Add(LadderRow.AddANI(ioSensor ?? ""));
                    }
                    else
                    {
                        result.Add(LadderRow.AddAND(ioSensor ?? ""));
                    }
                }
                result.Add(LadderRow.AddOR(label + (outNum + 17).ToString()));
                result.Add(LadderRow.AddAND(label + (outNum + 5).ToString()));
                result.Add(LadderRow.AddOUT(label + (outNum + 17).ToString()));
            }

            // 深当たりがある場合
            var operationTimerONWait = operationTimers.FirstOrDefault(t => t.Timer.TimerCategoryId == 5);

            if (operationTimerONWait != null)
            {
                result.Add(LadderRow.AddLD(label + (outNum + 17).ToString()));
                result.Add(LadderRow.AddANI(label + (outNum + 18).ToString()));
                result.AddRange(LadderRow.AddTimer(
                    operationTimerONWait.Timer.ProcessTimerDevice ?? "",
                    operationTimerONWait.Timer.TimerDevice ?? ""
                    ));
            }

            // M18
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
            // 深当たりタイマがある場合
            if (operationTimerONWait != null)
            {
                result.Add(LadderRow.AddAND(operationTimerONWait.Timer.TimerDevice));
            }
            result.Add(LadderRow.AddOR(label + (outNum + 18).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 17).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 18).ToString()));

            // 安定タイマがある場合
            var operationTimerStable = operationTimers.FirstOrDefault(t => t.Timer.TimerCategoryId == 2);

            if (operationTimerStable != null)
            {
                result.Add(LadderRow.AddLD(label + (outNum + 18).ToString()));
                result.Add(LadderRow.AddANI(label + (outNum + 19).ToString()));
                result.AddRange(LadderRow.AddTimer(
                    operationTimerStable.Timer.ProcessTimerDevice ?? "",
                    operationTimerStable.Timer.TimerDevice ?? ""
                    ));
            }

            // M19
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
            // 深当たりタイマがある場合
            if (operationTimerONWait != null)
            {
                result.Add(LadderRow.AddAND(operationTimerStable.Timer.TimerDevice));
            }
            result.Add(LadderRow.AddOR(label + (outNum + 19).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 18).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 19).ToString()));

            errors.AddRange(localErrors);
            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public static List<LadderCsvRow> Excitation(
                MnemonicDeviceWithOperation operation,
                List<MnemonicDeviceWithProcessDetail> details,
                List<MnemonicDeviceWithOperation> operations,
                List<MnemonicDeviceWithCylinder> cylinders,
                List<MnemonicTimerDeviceWithOperation> timers,
                List<IO> ioList,
                out List<OutputError> errors)
        {
            errors = new List<OutputError>(); // エラーリストの初期化
            var result = new List<LadderCsvRow>(); // 生成されるLadderCsvRowのリスト
            List<OutputError> localErrors = new();

            // 実際の処理ロジックをここに追加


            errors.AddRange(localErrors);
            return result; // 生成されたLadderCsvRowのリストを返す
        }
    }
}