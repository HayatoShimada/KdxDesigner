using KdxDesigner.Models;
using KdxDesigner.Utils.Process;
using KdxDesigner.Services;

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
            var repository = new AccessRepository();                        // ConnectionStringを取得するためのリポジトリ
            var mnemonicService = new MnemonicDeviceService(repository);    // MnemonicDeviceServiceのインスタンス
            var processDevices = new List<MnemonicDevice>();                // MnemonicDeviceテーブルのレコード
            var processDetailDevices = new List<MnemonicDevice>();          // MnemonicDeviceテーブルのレコード
            var allRows = new List<LadderCsvRow>();                         // ニモニック配列を格納するリスト
            var processBuilder = new BuildProcess();                        // BuildProcessのインスタンス
            errors = new List<OutputError>();                               // エラーリストの初期化


            // プロセスの開始デバイスと詳細の開始デバイスがnullの場合、エラーメッセージを表示
            if (processStartDevice == null)
            {
                MessageBox.Show("ProcessStartDeviceが入力されていません。");
                return allRows;
            }

            if (detailStartDevice == null)
            {
                MessageBox.Show("DetailStartDeviceが入力されていません。");
                return allRows;
            }

            // プロセスの必要デバイスを保存
            mnemonicService.SaveMnemonicDeviceProcess(processes, processStartDevice.Value, selectedCycle.PlcId);
            mnemonicService.SaveMnemonicDeviceProcessDetail(details, processStartDevice.Value, selectedCycle.PlcId);

            // MnemonicId = 1 だとProcessニモニックのレコード
            var devices = mnemonicService.GetMnemonicDevice(selectedCycle.PlcId);
            processDevices = devices
                .Where(m => m.MnemonicId == 1)
                .ToList();
            processDetailDevices = devices
                .Where(m => m.MnemonicId == 2)
                .ToList();

            // MnemonicDeviceとProcessのリストを結合
            // 並び順はProcess.Idで昇順
            var joinedProcessList = processDevices
                    .Join(
                        processes,
                        m => m.RecordId,
                        p => p.Id,
                        (m, p) => new MnemonicDeviceWithProcess
                        {
                            Mnemonic = m,
                            Process = p
                        })
                    .OrderBy(m => m.Process.Id)
                    .ToList();

            // MnemonicDeviceとProcessDetailのリストを結合
            // 並び順はProcessDetail.Idで昇順
            var joinedProcessDetailList = processDetailDevices
                    .Join(
                        details,
                        m => m.RecordId,
                        d => d.Id,
                        (m, d) => new MnemonicDeviceWithProcessDetail
                        {
                            Mnemonic = m,
                            Detail = d
                        })
                    .OrderBy(m => m.Detail.Id)
                    .ToList();

            // プロセスのニモニックを生成
            allRows = GenerateCsvRowsProcess(joinedProcessList, joinedProcessDetailList);

            // プロセス詳細のニモニックを生成
            return allRows;
        }

        public static List<LadderCsvRow> GenerateCsvRowsProcess(
            List<MnemonicDeviceWithProcess> list, 
            List<MnemonicDeviceWithProcessDetail> detail)
        {
            var mnemonic = new List<LadderCsvRow>();

            foreach (var pros in list)
            {
                switch(pros.Process.ProcessCategory)
                {
                    case "Normal": // 通常工程
                        mnemonic.AddRange(BuildProcess.BuildNormal(pros, detail));
                        break;
                    case "ResetAfter": // 工程まとめ
                        mnemonic.AddRange(BuildProcess.BuildNormal(pros, detail));
                        break;
                    case "IL": // センサON確認
                        mnemonic.AddRange(BuildProcess.BuildNormal(pros, detail));
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
