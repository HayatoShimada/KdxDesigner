using KdxDesigner.Models;
using KdxDesigner.Utils.Process;
using KdxDesigner.Services;

using System.Windows;
using KdxDesigner.Utils.ProcessDetail;
using KdxDesigner.Models.Define;

namespace KdxDesigner.Utils
{
    public static class ProcessBuilder
    {
        public static List<LadderCsvRow> GenerateAllLadderCsvRows(
            Cycle selectedCycle,
            int processStartDevice,
            int detailStartDevice,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithProcessDetail> details,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            LadderCsvRow.ResetKeyCounter();                     // 0から再スタート
            var allRows = new List<LadderCsvRow>();             // ニモニック配列を格納するリスト
            errors = new List<OutputError>();                   // エラーリストの初期化

            // プロセスのニモニックを生成
            allRows = GenerateCsvRowsProcess(processes, details);
            // プロセス詳細のニモニックを生成
            return allRows;
        }

        public static List<LadderCsvRow> GenerateCsvRowsProcess(
            List<MnemonicDeviceWithProcess> list, 
            List<MnemonicDeviceWithProcessDetail> details)
        {
            var mnemonic = new List<LadderCsvRow>();

            foreach (var pros in list)
            {
                switch(pros.Process.ProcessCategoryId)
                {
                    case 1:     // 通常工程
                        mnemonic.AddRange(BuildProcess.BuildNormal(pros, details));
                        break;
                    case 2:     // Single
                        //mnemonic.AddRange(BuildProcess.BuildNormal(pros, details));
                    case 3:     // リセット後工程 #issue16
                        //mnemonic.AddRange(BuildProcess.BuildNormal(pros, details));
                        break;
                    case 4:     // センサON確認
                        //mnemonic.AddRange(BuildProcess.BuildNormal(pros, details));
                        break;
                    default:
                        break;
                }
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
