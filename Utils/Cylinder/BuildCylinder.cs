using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Utils.MnemonicCommon;

using System.Diagnostics;

namespace KdxDesigner.Utils.Operation

{
    internal class BuildCylinder
    {
        public static List<LadderCsvRow> Excitation(
                MnemonicDeviceWithCylinder cylinder,
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

            string id = cylinder.Cylinder.Id.ToString();


            return result; // 生成されたLadderCsvRowのリストを返す
        }
    }
}