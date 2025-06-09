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
            List<Error> mnemonicError,
            List<ProsTime> prosTimes,
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
            result.AddRange(Common.GenerateM0(detailList, label, outNum.Value));

            // M2
            result.AddRange(Common.GenerateM2(label, outNum.Value));

            // M5
            result.AddRange(Common.GenerateM5(label, outNum.Value));

            // M6
            result.AddRange(Common.GenerateM6(operationTimers, label, outNum.Value));

            // M7
            // Start信号がある場合のみ回路を生成
            if (operation.Operation.Start != null)
            {
                result.AddRange(Common.GenerateM7(
                    operationTimers,
                    ioList,
                    operation,
                    plcId,
                    label,
                    outNum.Value,
                    out localErrors));
            }

            // M17
            if (operation.Operation.Finish != null)
            {
                result.AddRange(Common.GenerateM16(
                    operationTimers,
                    ioList,
                    operation,
                    plcId,
                    label,
                    outNum.Value,
                    out localErrors));
            }

            // M18
            result.AddRange(Common.GenerateM17(
                operationTimers,
                label,
                outNum.Value
            ));

            // M19
            result.AddRange(Common.GenerateM19(
                operationTimers,
                label,
                outNum.Value
            ));

            // エラー回路の生成
            result.AddRange(ErrorBuilder.Operation(
                operation.Operation,
                mnemonicError,
                label,
                outNum.Value,
                out localErrors
            ));

            // 工程タイムの生成
            result.AddRange(ProsTimeBuilder.Common(
                operation.Operation,
                prosTimes,
                label,
                outNum.Value,
                out localErrors
            ));

            errors.AddRange(localErrors);
            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public static List<LadderCsvRow> Excitation(
                MnemonicDeviceWithOperation operation,
                List<MnemonicDeviceWithProcessDetail> details,
                List<MnemonicDeviceWithOperation> operations,
                List<MnemonicDeviceWithCylinder> cylinders,
                List<MnemonicTimerDeviceWithOperation> timers,
                List<Error> mnemonicError,
                List<ProsTime> prosTimes,
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
            result.AddRange(Common.GenerateM0(detailList, label, outNum.Value));

            //M2
            result.Add(LadderRow.AddLD(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 2).ToString()));

            // M5
            result.AddRange(Common.GenerateM5(label, outNum.Value));

            // M6
            result.AddRange(Common.GenerateM6(operationTimers, label, outNum.Value));

            // M8
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddOR(label + (outNum + 2).ToString()));
            result.Add(LadderRow.AddANI(label + (outNum + 0).ToString()));
            result.Add(LadderRow.AddOR(label + (outNum + 8).ToString()));
            result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 8).ToString()));

            // M18
            var operationTimerONWait = operationTimers.FirstOrDefault(t => t.Timer.TimerCategoryId == 5);
            // 深当たりタイマがある場合
            if (operationTimerONWait != null)
            {
                result.Add(LadderRow.AddLD(label + (outNum + 8).ToString()));
                result.Add(LadderRow.AddANI(label + (outNum + 18).ToString()));
                result.AddRange(LadderRow.AddTimer(
                    operationTimerONWait.Timer.ProcessTimerDevice ?? "",
                    operationTimerONWait.Timer.TimerDevice ?? ""
                    ));
            }

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

            // M19
            result.AddRange(Common.GenerateM19(
                operationTimers,
                label,
                outNum.Value
            ));

            // 工程タイムの生成
            result.AddRange(ProsTimeBuilder.Common(
                operation.Operation,
                prosTimes,
                label,
                outNum.Value,
                out localErrors
            ));

            errors.AddRange(localErrors);
            return result; // 生成されたLadderCsvRowのリストを返す
        }
    }
}