using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services;
using KdxDesigner.Services.Error;
using KdxDesigner.Utils.MnemonicCommon;
using KdxDesigner.ViewModels;

using System.Diagnostics;

namespace KdxDesigner.Utils.Operation

{
    internal class BuildOperationSingle
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;

        public BuildOperationSingle(MainViewModel mainViewModel, IErrorAggregator errorAggregator, IIOAddressService ioAddressService)
        {
            _mainViewModel = mainViewModel;
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
        }


        public List<LadderCsvRow> Retention(
            MnemonicDeviceWithOperation operation,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<MnemonicTimerDeviceWithOperation> timers,
            List<Error> mnemonicError,
            List<ProsTime> prosTimes,
            List<IO> ioList)
        {
            // ここに単一工程の処理を実装
            var result = new List<LadderCsvRow>(); // 生成されるLadderCsvRowのリスト
            var label = operation.Mnemonic.DeviceLabel; // ラベルの取得
            var outNum = operation.Mnemonic.StartNum; // スタート番号の取得
            var operationTimers = timers.Where(t => t.Operation.Id == operation.Operation.Id).ToList();
            OperationFunction operationFunction = new(operation, operationTimers, ioList, details, _mainViewModel, _errorAggregator, _ioAddressService);


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
            result.AddRange(operationFunction.GenerateM0());

            // M2
            result.AddRange(operationFunction.GenerateM2());

            // M5
            result.AddRange(operationFunction.GenerateM5());

            // M6
            result.AddRange(operationFunction.GenerateM6());

            // M7
            if (operation.Operation.Start != null)
            {
                result.AddRange(operationFunction.GenerateM7());
            }

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

            result.AddRange(errorBuilder.Operation(
                operation.Operation,
                mnemonicError,
                label!,
                outNum!.Value
            ));

            ProsTimeBuilder prosTimeBuilder = new(_errorAggregator);

            // 工程タイムの生成
            result.AddRange(prosTimeBuilder.Common(
                operation.Operation,
                prosTimes,
                label!,
                outNum!.Value));

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> Excitation(
                MnemonicDeviceWithOperation operation,
                List<MnemonicDeviceWithProcessDetail> details,
                List<MnemonicDeviceWithOperation> operations,
                List<MnemonicDeviceWithCylinder> cylinders,
                List<MnemonicTimerDeviceWithOperation> timers,
                List<Error> mnemonicError,
                List<ProsTime> prosTimes,
                List<IO> ioList)
        {
            // ここに単一工程の処理を実装
            var result = new List<LadderCsvRow>(); // 生成されるLadderCsvRowのリスト
            var label = operation.Mnemonic.DeviceLabel; // ラベルの取得
            var outNum = operation.Mnemonic.StartNum; // スタート番号の取得
            var operationTimers = timers.Where(t => t.Operation.Id == operation.Operation.Id).ToList();
            OperationFunction operationFunction = new(operation, operationTimers, ioList, details, _mainViewModel, _errorAggregator, _ioAddressService);

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
            result.AddRange(operationFunction.GenerateM0());

            // M2
            result.Add(LadderRow.AddLD(label + (outNum + 1).ToString()));
            result.Add(LadderRow.AddOUT(label + (outNum + 2).ToString()));

            // M5
            result.AddRange(operationFunction.GenerateM5());

            // M6
            result.AddRange(operationFunction.GenerateM6());

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
            result.AddRange(operationFunction.GenerateM19());

            ProsTimeBuilder prosTimeBuilder = new(_errorAggregator);

            // 工程タイムの生成
            result.AddRange(prosTimeBuilder.Common(
                operation.Operation,
                prosTimes,
                label!,
                outNum!.Value));

            return result; // 生成されたLadderCsvRowのリストを返す
        }
    }
}