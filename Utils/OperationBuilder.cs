using KdxDesigner.Models;
using KdxDesigner.Utils.Process;
using KdxDesigner.Services;

using System.Windows;
using KdxDesigner.Utils.Operation;
using KdxDesigner.Models.Define;

namespace KdxDesigner.Utils
{
    public static class OperationBuilder
    {
        public static List<LadderCsvRow> GenerateAllLadderCsvRows(
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<MnemonicTimerDeviceWithOperation> timers,
            List<MnemonicSpeedDevice> speed,
            List<Error> mnemonicErrors,
            List<ProsTime> prosTimes,
            List<IO> ioList,
            int plcId,
            out List<OutputError> errors)
        {
            LadderCsvRow.ResetKeyCounter();                     // 0から再スタート
            errors = new List<OutputError>();                   // エラーリストの初期化
            var allRows = new List<LadderCsvRow>();
            List<OutputError> errorsForOperation = new(); // 各工程詳細のエラーリスト


            foreach (var operation in operations)
            {
                switch (operation.Operation.CategoryId)
                {
                    case 1 :                // 励磁
                        allRows.AddRange(BuildOperationSingle.Excitation(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            ioList,
                            out errors,
                            plcId
                            ));
                        errors.AddRange(errorsForOperation); // 修正: List<OutputError> を直接追加
                        break;
                    case 2 or 14 or 20:     // 保持
                        allRows.AddRange(BuildOperationSingle.Retention(
                            operation, 
                            details, 
                            operations, 
                            cylinders, 
                            timers, 
                            mnemonicErrors,
                            prosTimes,
                            ioList, 
                            out errors,
                            plcId
                            ));
                        errors.AddRange(errorsForOperation); // 修正: List<OutputError> を直接追加
                        break;
                    case 3 or 9 or 15:      // 速度変化1回
                        allRows.AddRange(BuildOperationSpeedChange.Inverter(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            speed,
                            ioList,
                            out errors,
                            plcId,
                            1
                            ));
                        errors.AddRange(errorsForOperation); // 修正: List<OutputError> を直接追加
                        break;

                    case 4 or 10 or 16:     // 速度変化2回
                        allRows.AddRange(BuildOperationSpeedChange.Inverter(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            speed,
                            ioList,
                            out errors,
                            plcId,
                            2
                            ));
                        errors.AddRange(errorsForOperation); // 修正: List<OutputError> を直接追加
                        break;

                    case 5 or 11 or 17:     // 速度変化3回
                        allRows.AddRange(BuildOperationSpeedChange.Inverter(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            speed,
                            ioList,
                            out errors,
                            plcId,
                            3
                            ));
                        errors.AddRange(errorsForOperation); // 修正: List<OutputError> を直接追加
                        break;

                    case 6 or 12 or 18:     // 速度変化4回
                        allRows.AddRange(BuildOperationSpeedChange.Inverter(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            speed,
                            ioList,
                            out errors,
                            plcId,
                            4
                            ));
                        errors.AddRange(errorsForOperation); // 修正: List<OutputError> を直接追加
                        break;

                    case 7 or 13 or 19:     // 速度変化5回
                        allRows.AddRange(BuildOperationSpeedChange.Inverter(
                            operation,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            speed,
                            ioList,
                            out errors,
                            plcId,
                            5
                            ));
                        errors.AddRange(errorsForOperation); // 修正: List<OutputError> を直接追加
                        break;

                    default:
                        break;
                }
            }

            errors = errorsForOperation.Distinct().ToList(); // 重複を排除
            return allRows;
        }

    }
}
