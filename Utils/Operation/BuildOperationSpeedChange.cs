using KdxDesigner.Models;
using KdxDesigner.Models.Define;
namespace KdxDesigner.Utils.Operation

{
    internal class BuildOperationSpeedChange
    {

        public static List<LadderCsvRow> Inverter(
            MnemonicDeviceWithOperation operation,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<MnemonicTimerDeviceWithOperation> timers,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            // ここに単一工程の処理を実装
            errors = new List<OutputError>(); // エラーリストの初期化
            var rows = new List<LadderCsvRow>(); // 生成されるLadderCsvRowのリスト
            List<OutputError> localErrors = new();

            // 実際の処理ロジックをここに追加


            errors.AddRange(localErrors);
            return rows; // 生成されたLadderCsvRowのリストを返す
        }
    }
}