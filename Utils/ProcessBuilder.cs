using KdxDesigner.Models;

namespace KdxDesigner.Utils
{
    public static class ProcessBuilder
    {
        public static List<LadderCsvRow> GenerateAllLadderCsvRows(
    List<ProcessDetailDto> details, List<IO> ioList)
        {
            var allRows = new List<LadderCsvRow>();

            foreach (var detail in details)
            {
                var rows = GenerateLadderCsvRows(detail, ioList);
                allRows.AddRange(rows);
            }

            return allRows;
        }

        public static List<LadderCsvRow> GenerateLadderCsvRows(ProcessDetailDto detail, List<IO> ioList)
        {
            var mnemonic = new List<LadderCsvRow>();

            switch (detail.CategoryId)
            {
                case 1: // 通常工程
                    mnemonic.AddRange(Process.NormalProcessBuilder.BuildNormalPattern(detail, ioList));
                    break;
                case 2: // 工程まとめ
                    mnemonic.AddRange(Process.NormalProcessBuilder.BuildNormalPattern(detail, ioList));
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

            return mnemonic;
        }
    }
}
