using KdxDesigner.Models;
using KdxDesigner.Models.Define;
namespace KdxDesigner.Utils.Operation

{
    internal class BuildOperationPositioning
    {

        public static List<LadderCsvRow> ServoPositioning(
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
            var result = new List<LadderCsvRow>(); // 生成されるLadderCsvRowのリスト
            List<OutputError> localErrors = new();

            // 実際の処理ロジックをここに追加
            errors.AddRange(localErrors);
            return result; // 生成されたLadderCsvRowのリストを返す
        }
    }
}