using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services;
using KdxDesigner.Services.Error;
using KdxDesigner.Utils.MnemonicCommon;
using KdxDesigner.ViewModels;
namespace KdxDesigner.Utils.Operation

{
    internal class BuildOperationSpeedChange
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;

        public BuildOperationSpeedChange(MainViewModel mainViewModel, IErrorAggregator errorAggregator, IIOAddressService ioAddressService)
        {
            _mainViewModel = mainViewModel;
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
        }

        // KdxDesigner.Utils.Operation.BuildOperationSpeedChange クラス内
        public List<LadderCsvRow> Inverter(
            MnemonicDeviceWithOperation operation,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<MnemonicTimerDeviceWithOperation> timers,
            List<KdxDesigner.Models.Error> mnemonicError,
            List<ProsTime> prosTimes,
            List<MnemonicSpeedDevice> speeds,
            List<IO> ioList,
            int speedChangeCount)
        {
            var result = new List<LadderCsvRow>();
            OperationFunction operationFunction = new(operation, timers, ioList, details, _mainViewModel, _errorAggregator, _ioAddressService);

            var label = operation.Mnemonic.DeviceLabel;
            var outNum = operation.Mnemonic.StartNum;

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

            if (!outNum.HasValue)
            {
                // outNum が null の場合はエラーを追加して終了
                CreateOperationError(operation, $"操作「{operation.Operation.OperationName}」(ID: {id}) の Mnemonic StartNum が設定されていません。");
                return result;
            }

            // outNum.Value を一度変数に格納して再利用
            var outNumValue = outNum.Value;

            result.AddRange(operationFunction.GenerateM0());
            result.AddRange(operationFunction.GenerateM2());
            result.AddRange(operationFunction.GenerateM5());
            result.AddRange(operationFunction.GenerateM6());

            if (operation.Operation.Start != null)
            {
                // ★★★ 修正: GenerateM7 がエラーを直接 _errorAggregator に追加するように変更 (OperationFunction側の修正も必要)
                result.AddRange(operationFunction.GenerateM7());
            }

            // M10 : 速度変化の処理
            for (int i = 0; i < speedChangeCount; i++)
            {
                if (i >= s_speedChangeConfigs.Count) continue;

                // ★★★ 修正: エラーハンドリングをTry-Getパターンとヘルパーメソッドに集約 ★★★
                if (!TryGetSpeedChangeParameters(i, operation, operationTimers, out var speedTimer, out var speedSensor))
                {
                    // TryGet内でエラーが追加されるため、ここでは何もしない
                    continue;
                }

                string? operationSpeed = string.Empty;

                // 速度変化ステップごとの処理
                switch (i)
                {
                    case 0: operationSpeed = operation.Operation.S1; break;
                    case 1: operationSpeed = operation.Operation.S2; break;
                    case 2: operationSpeed = operation.Operation.S3; break;
                    case 3: operationSpeed = operation.Operation.S4; break;
                    default:
                        // このケースは speedChangeConfigs.Count のチェックで基本的に到達しない
                        continue;
                }

                operationSpeed = FlowSpeedNumber(operationSpeed, operation, cylinders, i + 1);

                // ★★★ 修正: GenerateSpeed がエラーを直接 _errorAggregator に追加するように変更 (OperationFunction側の修正も必要)
                result.AddRange(operationFunction.GenerateSpeed(speedTimer, speedSensor, speeds, operationSpeed, i));
            }

            // ★★★ 修正: 各Builderがエラーを直接 _errorAggregator に追加するように変更 (各Builder側の修正も必要)
            // M16
            if (operation.Operation.Finish != null)
            {
                result.AddRange(operationFunction.GenerateM16());
            }
            // M17
            result.AddRange(operationFunction.GenerateM17());
            // M19
            result.AddRange(operationFunction.GenerateM19());

            // エラー回路の生成
            ErrorBuilder errorBuilder = new(_errorAggregator);
            result.AddRange(errorBuilder.Operation(operation.Operation, mnemonicError, label, outNumValue));

            // 工程タイムの生成
            ProsTimeBuilder prosTimeBuilder = new(_errorAggregator);
            result.AddRange(prosTimeBuilder.Common(operation.Operation, prosTimes, label, outNumValue));

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
        private static readonly List<SpeedChangeStepConfig> s_speedChangeConfigs = new()
        {
            new SpeedChangeStepConfig(9,  op => op.SS1, "SS1"),
            new SpeedChangeStepConfig(10, op => op.SS2, "SS2"),
            new SpeedChangeStepConfig(11, op => op.SS3, "SS3"),
            new SpeedChangeStepConfig(12, op => op.SS4, "SS4"),
            // 必要に応じてさらに追加
        };

        // BuildOperationSpeedChange クラス内
        private bool TryGetSpeedChangeParameters(
    int speedChangeIndex,
    MnemonicDeviceWithOperation operation,
    List<MnemonicTimerDeviceWithOperation> operationTimers,
    out MnemonicTimerDeviceWithOperation? speedTimer,
    out string? speedSensor)
        {
            speedTimer = null;
            speedSensor = null;

            if (speedChangeIndex < 0 || speedChangeIndex >= s_speedChangeConfigs.Count)
            {
                // 通常は到達しないが、念のため
                CreateOperationError(operation, $"不正な速度変化ステップ インデックス: {speedChangeIndex + 1}");
                return false;
            }

            var config = s_speedChangeConfigs[speedChangeIndex];

            try
            {
                speedTimer = operationTimers.SingleOrDefault(t => t.Timer.TimerCategoryId == config.TimerCategoryId);
                if (speedTimer == null)
                {
                    CreateOperationError(operation, $"操作「{operation.Operation.OperationName}」(ID: {operation.Operation.Id}) で、カテゴリID {config.TimerCategoryId} (速度変化 {speedChangeIndex + 1}) のタイマーが設定されていません。");
                    return false; // タイマーがない場合は失敗
                }
            }
            catch (InvalidOperationException)
            {
                CreateOperationError(operation, $"操作「{operation.Operation.OperationName}」(ID: {operation.Operation.Id}) で、カテゴリID {config.TimerCategoryId} (速度変化 {speedChangeIndex + 1}) のタイマーが複数設定されています。");
                return false;
            }

            speedSensor = config.SensorAccessor(operation.Operation);
            if (string.IsNullOrEmpty(speedSensor))
            {
                CreateOperationError(operation, $"操作「{operation.Operation.OperationName}」(ID: {operation.Operation.Id}) で、速度変化 {speedChangeIndex + 1} ({config.SensorPropertyName}) のセンサーが設定されていません。");
                return false; // センサーがない場合は失敗
            }

            return true; // 全て成功
        }


        // ★★★ 修正: out パラメータを無くし、戻り値もvoidに変更 ★★★
        private void CreateOperationError(MnemonicDeviceWithOperation operation, string message)
        {
            var error = new OutputError
            {
                Message = message,
                DetailName = operation.Operation?.OperationName ?? "N/A",
                MnemonicId = (int)MnemonicType.Operation,
                ProcessId = operation.Operation?.Id ?? 0
            };
            _errorAggregator.AddError(error);
        }

        private string FlowSpeedNumber(
            string? operationSpeed,
            MnemonicDeviceWithOperation operation,
            List<MnemonicDeviceWithCylinder> cylinders,
            int stepNumber // エラーメッセージ用にステップ番号を追加
            )
        {
            if (string.IsNullOrEmpty(operationSpeed))
            {
                CreateOperationError(operation, $"操作「{operation.Operation.OperationName}」(ID: {operation.Operation.Id}) の速度変化ステップ {stepNumber} (S{stepNumber}) が設定されていません。");
                return string.Empty;
            }

            if (operationSpeed.Contains("A"))
            {
                return operationSpeed.Replace("A", "");
            }

            if (operationSpeed.Contains("B"))
            {
                string speedValueStr = operationSpeed.Replace("B", "");
                if (!int.TryParse(speedValueStr, out int speedValue))
                {
                    CreateOperationError(operation, $"操作「{operation.Operation.OperationName}」(ID: {operation.Operation.Id}) の速度 {operationSpeed} は不正な形式です。");
                    return string.Empty;
                }

                var flow = cylinders.SingleOrDefault(c => c.Cylinder.Id == operation.Operation.CYId)?.Cylinder?.FlowType;
                switch (flow)
                {
                    case "A5:B5": return (speedValue + 5).ToString();
                    case "A6:B4": return (speedValue + 6).ToString();
                    case "A7:B3": return (speedValue + 8).ToString();
                    default:
                        CreateOperationError(operation, $"操作「{operation.Operation.OperationName}」(ID: {operation.Operation.Id}) の FlowType '{flow}' は未対応です。");
                        return speedValueStr; // 元の数値を返す
                }
            }
            return operationSpeed;
        }
    }
}