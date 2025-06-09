using KdxDesigner.Models;
using KdxDesigner.Utils.Process;
using KdxDesigner.Services;

using System.Windows;
using KdxDesigner.Utils.Operation;
using KdxDesigner.Models.Define;

namespace KdxDesigner.Utils
{
    public static class CylinderBuilder
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

            foreach (var cylinder in cylinders)
            {
                switch (cylinder.Cylinder.DriveSub)
                {
                    case 1 :                // 励磁
                        allRows.AddRange(BuildCylinder.Excitation(
                            cylinder,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            ioList,
                            out errors,
                            plcId));
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
