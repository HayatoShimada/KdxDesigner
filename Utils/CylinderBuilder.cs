using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Utils.Cylinder;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Utils
{
    public class CylinderBuilder
    {
        private readonly MainViewModel _mainViewModel;
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
            LadderCsvRow.ResetKeyCounter();
            errors = new List<OutputError>();
            var result = new List<LadderCsvRow>();
            var builder = new BuildCylinder(_mainViewModel);
            List<OutputError> errorsForOperation = new();

            foreach (var cylinder in cylinders)
            {
                switch (cylinder.Cylinder.DriveSub)
                {
                    case 1:                // 励磁
                        result.AddRange(builder.Valve1(
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
            return result;
        }

    }
}
