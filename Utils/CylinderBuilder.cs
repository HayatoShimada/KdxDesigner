using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services;
using KdxDesigner.Utils.Cylinder;
using KdxDesigner.Utils.Process;
using KdxDesigner.ViewModels;

using System.Windows;

namespace KdxDesigner.Utils
{
    public class CylinderBuilder
    {

        private readonly MainViewModel _mainViewModel;

        // コンストラクタでMainViewModelをインジェクト
        public CylinderBuilder(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        public List<LadderCsvRow> GenerateAllLadderCsvRows(
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
                        allRows.AddRange(BuildCylinder.Valve1(
                            cylinder,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            ioList,
                            out errors,
                            plcId,
                            _mainViewModel));
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
