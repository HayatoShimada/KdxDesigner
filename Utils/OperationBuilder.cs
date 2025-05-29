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
            Cycle selectedCycle,
            int processStartDevice,
            int detailStartDevice,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<MnemonicTimerDeviceWithOperation> timers,
            List<IO> ioList,
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
                    case 1:     // 通常工程
                        allRows.AddRange(BuildOperationSingle.Retention(
                            operation, 
                            details, 
                            operations, 
                            cylinders, 
                            timers, 
                            ioList, 
                            out errors));
                        errors.AddRange(errorsForOperation); // 修正: List<OutputError> を直接追加

                        break;
                    case 2:     // 
                        break;
                    case 3:     // 
                        break;
                    case 4:     // 
                        break;
                    default:
                        break;
                }
            }

            // プロセス詳細のニモニックを生成
            errors = errorsForOperation.Distinct().ToList(); // 重複を排除
            return allRows;
            // プロセス詳細のニモニックを生成
        }

    }
}
