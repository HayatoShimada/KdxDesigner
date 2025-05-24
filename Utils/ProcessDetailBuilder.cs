using KdxDesigner.Models;
using KdxDesigner.Utils.Process;
using KdxDesigner.Services;

using System.Windows;
using KdxDesigner.Utils.ProcessDetail;
using KdxDesigner.Models.Define;

namespace KdxDesigner.Utils
{
    public static class ProcessDetailBuilder
    {
        public static List<LadderCsvRow> GenerateAllLadderCsvRows(
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            LadderCsvRow.ResetKeyCounter();                     // 0から再スタート
            var allRows = new List<LadderCsvRow>();             // ニモニック配列を格納するリスト
            errors = new List<OutputError>();                   // エラーリストの初期化

            foreach (var detail in details)
            {
                switch (detail.Detail.CategoryId)
                {
                    case 1: // 通常工程
                        allRows.AddRange(BuildDetail.BuildDetailNormal(detail, processes, operations, cylinders, ioList, out errors));
                        break;
                    case 2: // 工程まとめ
                        allRows.AddRange(BuildDetail.BuildDetailSummarize(detail, processes, operations, cylinders, ioList, out errors));
                        break;
                    case 3: // センサON確認
                        break;
                    case 4: // センサOFF確認
                        break;
                    case 5: // 工程分岐
                        break;
                    case 6: // 工程合流
                        break;
                    case 7: // サーボ座標指定
                        break;
                    case 8: // サーボ番号指定
                        break;
                    case 9: // INV座標指定
                        break;
                    case 10: // IL待ち
                        break;
                    case 11: // リセット工程開始
                        break;
                    case 12: // リセット工程完了
                        break;
                    case 13: // 工程OFF確認
                        break;
                    default:
                        break;
                }
            }
            // プロセス詳細のニモニックを生成
            return allRows;
        }

    }
}
