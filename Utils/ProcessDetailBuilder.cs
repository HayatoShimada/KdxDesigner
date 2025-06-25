using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services;
using KdxDesigner.Services.Error;
using KdxDesigner.Utils.ProcessDetail;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Utils
{
    public class ProcessDetailBuilder
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;

        public ProcessDetailBuilder(MainViewModel mainViewModel, IErrorAggregator errorAggregator, IIOAddressService ioAddressService)
        {
            _mainViewModel = mainViewModel; // MainViewModelのインスタンスを取得
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
        }

        public List<LadderCsvRow> GenerateAllLadderCsvRows(
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            List<MnemonicTimerDeviceWithDetail> detailTimers)
        {
            LadderCsvRow.ResetKeyCounter();                     // 0から再スタート
            var allRows = new List<LadderCsvRow>();             // ニモニック配列を格納するリスト
            List<OutputError> errorsForDetail = new(); // 各工程詳細のエラーリスト
            BuildDetail buildDetail = new(_mainViewModel, _ioAddressService, _errorAggregator); // BuildDetailのインスタンスを生成
            int plcId = _mainViewModel.SelectedPlc!.Id;
            foreach (var detail in details)
            {
                switch (detail.Detail.CategoryId)
                {
                    case 1:     // 通常工程
                        allRows.AddRange(buildDetail.BuildDetailNormal(detail, details, processes, operations, cylinders, ioList));
                        break;
                    case 2:     // 工程まとめ
                        allRows.AddRange(buildDetail.BuildDetailSummarize(detail, details, processes, operations, cylinders, ioList));
                        break;
                    case 3:     // センサON確認
                        allRows.AddRange(buildDetail.BuildDetailSensorON(detail, details, processes, operations, cylinders, ioList));
                        break;
                    case 4:     // センサOFF確認
                        allRows.AddRange(buildDetail.BuildDetailSensorOFF(detail, details, processes, operations, cylinders, ioList));
                        break;
                    case 5:     // 工程分岐
                        allRows.AddRange(buildDetail.BuildDetailBranch(detail, details, processes, operations, cylinders, ioList));
                        break;
                    case 6:     // 工程合流
                        allRows.AddRange(buildDetail.BuildDetailMerge(detail, details, processes, operations, cylinders, ioList));
                        break;
                    case 7:     // サーボ座標指定
                        break;
                    case 8:     // サーボ番号指定
                        break;
                    case 9:     // INV座標指定
                        break;
                    case 10:    // IL待ち
                        allRows.AddRange(buildDetail.BuildDetailILWait(detail, details, processes, operations, cylinders, ioList));
                        break;
                    case 11:    // リセット工程開始
                        break;
                    case 12:    // リセット工程完了
                        break;
                    case 13:    // 工程OFF確認
                        break;
                    case 15:    // 期間工程
                        allRows.AddRange(buildDetail.BuildDetailSeason(detail, details, processes, operations, cylinders, ioList));
                        break;
                    case 16:    // タイマ工程
                        allRows.AddRange(buildDetail.BuildDetailTimerProcess(detail, details, processes, operations, cylinders, ioList, detailTimers));
                        break;                    
                    case 17:    // タイマ
                        allRows.AddRange(buildDetail.BuildDetailTimer(detail, details, processes, operations, cylinders, ioList, detailTimers));
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
