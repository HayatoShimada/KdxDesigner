using KdxDesigner.Models;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KdxDesigner.Utils.Process
{
    public static class ProcessBuilder
    {
        public static List<LadderCsvRow> GenerateAllLadderCsvRows(
            Cycle selectedCycle,
            List<Models.Process> processes,
            List<ProcessDetailDto> details,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            var allRows = new List<LadderCsvRow>();
            errors = new List<OutputError>();

            // 必要に応じて Cycle のログ出力やフィルタを行う
            var targetProcessIds = processes
                .Where(p => p.CycleId == selectedCycle.Id)
                .Select(p => p.Id)
                .ToHashSet();

            foreach (var detail in details.Where(d => d.ProcessId.HasValue && targetProcessIds.Contains(d.ProcessId.Value)))
            {
                try
                {
                    var rows = GenerateLadderCsvRows(detail, ioList);
                    allRows.AddRange(rows);
                }
                catch (Exception ex)
                {
                    errors.Add(new OutputError
                    {
                        DetailName = detail.DetailName,
                        Message = ex.Message,
                        ProcessId = detail.ProcessId
                    });
                }
            }

            return allRows;
        }


        public static List<LadderCsvRow> GenerateLadderCsvRows(ProcessDetailDto detail, List<IO> ioList)
        {
            var mnemonic = new List<LadderCsvRow>();

            switch (detail.CategoryId)
            {
                case 1: // 通常工程
                    mnemonic.AddRange(NormalProcessBuilder.BuildNormalPattern(detail, ioList));
                    break;
                case 2: // 工程まとめ
                    mnemonic.AddRange(NormalProcessBuilder.BuildNormalPattern(detail, ioList));
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
