using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Utils.MnemonicCommon;
namespace KdxDesigner.Utils.Operation

{
    internal class BuildOperationSpeedChange
    {

        // KdxDesigner.Utils.Operation.BuildOperationSpeedChange クラス内
        public static List<LadderCsvRow> Inverter(
            MnemonicDeviceWithOperation operation,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<MnemonicTimerDeviceWithOperation> timers,
            List<KdxDesigner.Models.Error> mnemonicError, // KdxDesigner.Models.Error 型であると明示
            List<ProsTime> prosTimes,
            List<IO> ioList,
            out List<OutputError> errors,
            int plcId,
            int speedChangeCount)
        {
            errors = new List<OutputError>();
            var result = new List<LadderCsvRow>();
            List<OutputError> localErrors = new(); // このメソッド内で発生したエラーを集約

            var label = operation.Mnemonic.DeviceLabel;
            var outNum = operation.Mnemonic.StartNum; // Nullable<int> の可能性を考慮 (例: outNum.Value)

            List<MnemonicTimerDeviceWithOperation> operationTimers = timers
                .Where(t => t.Operation.Id == operation.Operation.Id).ToList();

            string id = operation.Operation.Id.ToString();
            if (string.IsNullOrEmpty(operation.Operation.OperationName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + operation.Operation.OperationName));
            }

            var detailList = details.Where(d => d.Detail.OperationId == operation.Operation.Id).ToList();
            if (outNum.HasValue) // outNum が Nullable の場合、Value を使う前に HasValue を確認
            {
                result.AddRange(Common.GenerateM0(detailList, label, outNum.Value));
                result.AddRange(Common.GenerateM2(label, outNum.Value));
                result.AddRange(Common.GenerateM5(label, outNum.Value));
                // Common.GenerateM6 がエラーを返す場合、同様に処理
                // List<OutputError> m6Errors;
                // result.AddRange(Common.GenerateM6(operationTimers, label, outNum.Value, out m6Errors));
                // localErrors.AddRange(m6Errors);
                // 上記はGenerateM6のシグネチャがエラーリストを返す場合。現在のコードからは不明。
                // もし現在のシグネチャのままなら:
                result.AddRange(Common.GenerateM6(operationTimers, label, outNum.Value));


                if (operation.Operation.Start != null)
                {
                    List<OutputError> m7Errors; // Common.GenerateM7 からのエラーを個別に受け取る
                    result.AddRange(Common.GenerateM7(
                        operationTimers, ioList, operation, plcId, label, outNum.Value,
                        out m7Errors)); // out パラメータでエラーリストを受け取る
                    if (m7Errors != null) localErrors.AddRange(m7Errors);
                }

                // M10 : 速度変化の処理
                for (int i = 0; i < speedChangeCount; i++)
                {
                    if (i >= s_speedChangeConfigs.Count) // 設定されている速度変化のステップ数を超えた場合
                    {
                        localErrors.Add(CreateOperationError(
                            operation, 
                            $"定義されている速度変化ステップ数 ({s_speedChangeConfigs.Count}) を超えています (要求: {i + 1})。"));
                        continue;
                    }

                    MnemonicTimerDeviceWithOperation speedTimer;
                    string speedSensor;
                    OutputError paramError;

                    if (!TryGetSpeedChangeParameters(i, operation, operationTimers, out speedTimer, out speedSensor, out paramError))
                    {
                        if (paramError != null)
                        {
                            localErrors.Add(paramError);
                        }
                        // エラー発生時はこの速度変化ステップの処理をスキップ
                        continue;
                    }

                    if (speedTimer == null)
                    {
                        var config = s_speedChangeConfigs[i]; // エラーメッセージ用にconfigを取得
                        localErrors.Add(CreateOperationError(
                            operation,
                            $"操作「{operation.Operation.OperationName}」(ID: {operation.Operation.Id}) で、カテゴリID {config.TimerCategoryId} (速度変化 {i + 1}) のタイマーが設定されていません。"
                        ));
                        continue;
                    }

                    if (string.IsNullOrEmpty(speedSensor))
                    {
                        var config = s_speedChangeConfigs[i]; // エラーメッセージ用にconfigを取得
                        localErrors.Add(CreateOperationError(
                            operation,
                            $"操作「{operation.Operation.OperationName}」(ID: {operation.Operation.Id}) で、速度変化 {i + 1} ({config.SensorPropertyName}) のセンサーが設定されていません。"
                        ));
                        continue;
                    }

                    List<OutputError> speedGenErrors; // Common.GenerateSpeed からのエラーを個別に受け取る
                    result.AddRange(Common.GenerateSpeed(
                        operation, speedTimer, ioList, speedSensor, plcId, label, outNum.Value,
                        out speedGenErrors, // out パラメータでエラーリストを受け取る
                        i));
                    if (speedGenErrors != null) localErrors.AddRange(speedGenErrors);
                }

                // M16
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

                // M17
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

            }
            else
            {
                // outNum が null の場合の処理 (エラー追加など)
                localErrors.Add(CreateOperationError(operation, $"操作「{operation.Operation.OperationName}」(ID: {operation.Operation.Id}) の Mnemonic StartNum が設定されていません。"));
            }


            errors.AddRange(localErrors); // 最後に集約したエラーを out パラメータに設定
            return result;
        }

        public class SpeedChangeStepConfig
        {
            public int TimerCategoryId { get; }
            public Func<KdxDesigner.Models.Operation, string> SensorAccessor { get; } // KdxDesigner.Models.Define.Operation を適切な型に置き換えてください
            public string SensorPropertyName { get; } // エラーメッセージ用

            public SpeedChangeStepConfig(int categoryId, Func<KdxDesigner.Models.Operation, string> accessor, string sensorPropertyName)
            {
                TimerCategoryId = categoryId;
                SensorAccessor = accessor;
                SensorPropertyName = sensorPropertyName;
            }
        }

        // BuildOperationSpeedChange クラス内
        private static readonly List<SpeedChangeStepConfig> s_speedChangeConfigs = new List<SpeedChangeStepConfig>
        {
            new SpeedChangeStepConfig(9,  op => op.SS1, "SS1"),
            new SpeedChangeStepConfig(10, op => op.SS2, "SS2"),
            new SpeedChangeStepConfig(11, op => op.SS3, "SS3"),
            new SpeedChangeStepConfig(12, op => op.SS4, "SS4"),
            // 必要に応じてさらに追加
        };

        // BuildOperationSpeedChange クラス内
        private static bool TryGetSpeedChangeParameters(
            int speedChangeIndex,
            MnemonicDeviceWithOperation operation,
            List<MnemonicTimerDeviceWithOperation> operationTimers,
            out MnemonicTimerDeviceWithOperation speedTimer,
            out string speedSensor,
            out OutputError error)
        {
            speedTimer = null;
            speedSensor = string.Empty;
            error = null;

            if (speedChangeIndex < 0 || speedChangeIndex >= s_speedChangeConfigs.Count)
            {
                // speedChangeCount が s_speedChangeConfigs の範囲外の場合のエラー処理
                // (通常、呼び出し元のループ条件でこれは発生しないはず)
                error = CreateOperationError(operation, $"速度変化ステップ {speedChangeIndex + 1} の設定が見つかりません。");
                return false;
            }

            var config = s_speedChangeConfigs[speedChangeIndex];

            try
            {
                speedTimer = operationTimers.SingleOrDefault(t => t.Timer.TimerCategoryId == config.TimerCategoryId);
            }
            catch (InvalidOperationException) // 具体的な例外をキャッチ
            {
                error = CreateOperationError(
                    operation,
                    $"操作「{operation.Operation.OperationName}」(ID: {operation.Operation.Id}) で、カテゴリID {config.TimerCategoryId} (速度変化 {speedChangeIndex + 1}) のタイマーが複数設定されています。"
                );
                return false;
            }

            // speedSensor の取得
            if (operation.Operation != null)
            {
                speedSensor = config.SensorAccessor(operation.Operation) ?? string.Empty;
            }
            else
            {
                // operation.Operation が null の場合の処理 (通常はありえないが念のため)
                error = CreateOperationError(operation, $"操作データ (operation.Operation) が null です。");
                return false;
            }

            return true;
        }

        // OutputError オブジェクト生成のヘルパーメソッド (任意)
        private static OutputError CreateOperationError(MnemonicDeviceWithOperation operation, string message)
        {
            return new OutputError
            {
                Message = message,
                DetailName = operation.Operation?.OperationName ?? "N/A",
                MnemonicId = (int)MnemonicType.Operation, // KdxDesigner.Utils.MnemonicCommon.MnemonicType を想定
                ProcessId = operation.Operation?.Id ?? 0 // operation.Operation.Id が int であると仮定
            };
        }
    }
}