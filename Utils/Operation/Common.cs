using KdxDesigner.Models.Define;
using KdxDesigner.Utils.MnemonicCommon;
using KdxDesigner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Utils.Operation
{
    internal class Common
    {
      
           
        public static List<LadderCsvRow> GenerateM0(
            List<MnemonicDeviceWithProcessDetail> detailList,
            string label,
            int outNum
            )
        {
            var result = new List<LadderCsvRow>();
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

            return result;
        }

        public static List<LadderCsvRow> GenerateM2(
            string label,
            int outNum
            )
        {
            var result = new List<LadderCsvRow>();
            result.Add(LadderRow.AddLD(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddSET(label + (outNum + 2).ToString()));

            return result;
        }

        public static List<LadderCsvRow> GenerateM5(
            string label,
            int outNum
            )
        {
            var result = new List<LadderCsvRow>();
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

            return result;
        }

        public static List<LadderCsvRow> GenerateM6(
            List<MnemonicTimerDeviceWithOperation> operationTimers,
            string label,
            int outNum
            )
        {
            var result = new List<LadderCsvRow>();
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

            return result;
        }

        public static List<LadderCsvRow> GenerateM7(
            List<MnemonicTimerDeviceWithOperation> operationTimers,
            List<IO> ioList,
            MnemonicDeviceWithOperation operation,
            int plcId,
            string label,
            int outNum,
            out List<OutputError> localErrors
            )
        {
            var result = new List<LadderCsvRow>();
            // 開始待ちタイマがある場合
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

            return result;
        }

        public static List<LadderCsvRow> GenerateSpeed(
            MnemonicDeviceWithOperation operation,
            MnemonicTimerDeviceWithOperation operationTimer,
            List<IO> ioList,
            string speedSensor,
            int plcId,
            string label,
            int outNum,
            out List<OutputError> localErrors,
            int speedCount
            )
        {
            var result = new List<LadderCsvRow>();
            // 開始待ちタイマがある場合
            var ioSensor = IOAddress.FindByIOText(ioList, speedSensor, plcId, out localErrors);

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
                    result.Add(LadderRow.AddLD(ioSensor));
                }
                else
                {
                    result.Add(LadderRow.AddLD(ioSensor));

                }
            }

            result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
            result.Add(LadderRow.AddANI(label + (outNum + 10 + speedCount).ToString()));
            result.AddRange(LadderRow.AddTimer(
                    operationTimer.Timer.ProcessTimerDevice ?? "",
                    operationTimer.Timer.TimerDevice ?? ""
                    ));

            // M10 + sppeedCount
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
            result.Add(LadderRow.AddAND(operationTimer.Timer.ProcessTimerDevice));
            result.Add(LadderRow.AddOR(label + (outNum + 5).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 10 + speedCount).ToString()));

            return result;
        }

        public static List<LadderCsvRow> GenerateM16(
            List<MnemonicTimerDeviceWithOperation> operationTimers,
            List<IO> ioList,
            MnemonicDeviceWithOperation operation,
            int plcId,
            string label,
            int outNum,
            out List<OutputError> localErrors
            )
        {
            var result = new List<LadderCsvRow>();
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
            result.Add(LadderRow.AddOR(label + (outNum + 16).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 5).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 16).ToString()));

            return result;
        }

        public static List<LadderCsvRow> GenerateM17(
            List<MnemonicTimerDeviceWithOperation> operationTimers,
            string label,
            int outNum
            )
        {
            var operationTimerONWait = operationTimers.FirstOrDefault(t => t.Timer.TimerCategoryId == 5);
            var result = new List<LadderCsvRow>();
            // 開始待ちタイマがある場合
            if (operationTimerONWait != null)
            {
                result.Add(LadderRow.AddLD(label + (outNum + 16).ToString()));
                result.Add(LadderRow.AddANI(label + (outNum + 17).ToString()));
                result.AddRange(LadderRow.AddTimer(
                    operationTimerONWait.Timer.ProcessTimerDevice ?? "",
                    operationTimerONWait.Timer.TimerDevice ?? ""
                    ));
            }

            // M17
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
            // 深当たりタイマがある場合
            if (operationTimerONWait != null)
            {
                result.Add(LadderRow.AddAND(operationTimerONWait.Timer.TimerDevice));
            }
            result.Add(LadderRow.AddOR(label + (outNum + 17).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 16).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 17).ToString()));

            return result;
        }

        public static List<LadderCsvRow> GenerateM19(
            List<MnemonicTimerDeviceWithOperation> operationTimers,
            string label,
            int outNum
            )
        {
            var result = new List<LadderCsvRow>();
            var operationTimerStable = operationTimers.FirstOrDefault(t => t.Timer.TimerCategoryId == 2);
            var operationTimerONWait = operationTimers.FirstOrDefault(t => t.Timer.TimerCategoryId == 5);

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

            return result;
        }
    }
}
