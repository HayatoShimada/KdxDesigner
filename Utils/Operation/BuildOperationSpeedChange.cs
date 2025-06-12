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
            var operationDetails = details.Where(d => d.Detail.OperationId == operation.Operation.Id).ToList();
            OperationFunction operationFunction = new(operation, timers, cylinders, ioList, operationDetails, _mainViewModel, _errorAggregator, _ioAddressService);
            OperationHelper helper = new(_mainViewModel, _errorAggregator, _ioAddressService);

            string label = string.Empty;
            int outNum = 0;

            if (operation != null 
                && operation.Mnemonic != null 
                && operation.Mnemonic.DeviceLabel != null)
            {
                label = operation.Mnemonic.DeviceLabel;
                outNum = operation.Mnemonic.StartNum;
                helper.CreateOperationError(operation, $"Mnemonicデバイスが設定されていません。");
                return result;
            }
           

            List<MnemonicTimerDeviceWithOperation> operationTimers = timers
                .Where(t => t.Operation.Id == operation!.Operation.Id).ToList();

            string id = operation!.Operation.Id.ToString();
            if (string.IsNullOrEmpty(operation.Operation.OperationName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + operation.Operation.OperationName));
            }

            var detailList = details.Where(d => d.Detail.OperationId == operation.Operation.Id).ToList();

            // outNum.Value を一度変数に格納して再利用
            var outNumValue = outNum;

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
            result.AddRange(operationFunction.SpeedCheck(speeds, speedChangeCount, operationTimers));

            // M16
            if (operation.Operation.Finish != null)
            {
                result.AddRange(operationFunction.GenerateM16());
            }
            // M17
            result.AddRange(operationFunction.GenerateM17());
            // M19
            result.AddRange(operationFunction.GenerateM19());
            // Reset信号の生成
            result.AddRange(operationFunction.GenerateReset());


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

        


        
    }
}