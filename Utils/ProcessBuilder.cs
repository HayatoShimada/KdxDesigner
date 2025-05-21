using KdxDesigner.Models;
using KdxDesigner.Utils.Process;

using System.Windows;

namespace KdxDesigner.Utils
{
    public static class ProcessBuilder
    {
        public static List<LadderCsvRow> GenerateAllLadderCsvRows(
            Cycle selectedCycle,
            int? processStartDevice,
            int? detailStartDevice,
            List<Models.Process> processes,
            List<ProcessDetailDto> details,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            var allRows = new List<LadderCsvRow>();
            errors = new List<OutputError>();

            int processNum = 0;
            int detailNum = 0;

            if (processStartDevice == null)
            {
                MessageBox.Show("ProcessStartDeviceが入力されていません。");
                processNum = processStartDevice!.Value;
                return allRows;
            }

            if (detailStartDevice == null)
            {
                MessageBox.Show("DetailStartDeviceが入力されていません。");
                detailNum = detailStartDevice!.Value;
                return allRows;
            }


            // 必要に応じて Cycle のログ出力やフィルタを行う
            var targetProcessIds = processes
                .Where(p => p.CycleId == selectedCycle.Id)
                .Select(p => p.Id)
                .ToHashSet();

            

            // Processからニモニックを生成
            int count = 0;
            foreach (var process in processes)
            {
                try
                {
                    var rows = GenerateCsvRows(process, processNum, detailNum, count);

                }
                catch (Exception ex)
                {
                    errors.Add(new OutputError
                    {

                    });
                }

            }

            // ProcessDetailからニモニックを生成
            foreach (var detail in details.Where(d => d.ProcessId.HasValue && targetProcessIds.Contains(d.ProcessId.Value)))
            {
                try
                {
                    var rows = GenerateCsvRowsDetail(detail, ioList);
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

        public static List<LadderCsvRow> GenerateCsvRows(
            Models.Process process, 
            int processStartNum, 
            int detailStartNum)
        {
            var mnemonic = new List<LadderCsvRow>();

            switch (process.ProcessCategory)
            {
                case "Normal": // 通常工程
                    mnemonic.AddRange(BuildProcess.BuildNormal(process, processStartNum, detailStartNum));
                    break;
                case "ResetAfter": // 工程まとめ
                    mnemonic.AddRange(BuildProcess.BuildResetAfter(process, processStartNum, detailStartNum));
                    break;
                case "IL": // センサON確認
                    mnemonic.AddRange(BuildProcess.BuildIL(process, processStartNum, detailStartNum));
                    break;
                default:
                    break;
            }

            return mnemonic;
        }


        public static List<LadderCsvRow> GenerateCsvRowsDetail(ProcessDetailDto detail, List<IO> ioList)
        {
            var mnemonic = new List<LadderCsvRow>();

            switch (detail.CategoryId)
            {
                case 1: // 通常工程
                    mnemonic.AddRange(BuildDetail.BuildNormalPattern(detail, ioList));
                    break;
                case 2: // 工程まとめ
                    mnemonic.AddRange(BuildDetail.BuildNormalPattern(detail, ioList));
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
