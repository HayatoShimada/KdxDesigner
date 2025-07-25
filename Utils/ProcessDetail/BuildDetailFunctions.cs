﻿using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services;
using KdxDesigner.Services.Access;
using KdxDesigner.Services.Error;
using KdxDesigner.Services.IOAddress;
using KdxDesigner.Utils.MnemonicCommon;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Utils.ProcessDetail
{
    /// <summary>
    /// ProcessDetailのビルド関数を提供するクラス。
    /// </summary>
    /// <remarks>
    /// 継承している <see cref="BuildDetail"/> クラスは、processDetailの出力単位ビルド機能を提供するのに対し、
    /// このクラスは各アウトコイル単位のビルド機能を提供します。
    /// </remarks>
    internal class BuildDetailFunctions : BuildDetail
    {
        // --- このクラス固有のフィールド ---
        private readonly MnemonicDeviceWithProcessDetail _detail;
        private readonly MnemonicDeviceWithProcess _process;
        private readonly string _label;
        private readonly int _outNum;


        /// <summary>
        /// 新しい BuildDetailFunctions のインスタンスを初期化します。
        /// </summary>
        /// <param name="detail">処理対象の工程詳細データ</param>
        /// <param name="process">処理対象の工程データ</param>
        /// <param name="mainViewModel">MainViewからの初期値</param>
        /// <param name="ioAddressService">IO検索用のサービス</param>
        /// <param name="errorAggregator">エラー出力用のサービス</param>
        /// <param name="repository">ACCESSファイル検索用のリポジトリ</param>
        /// <param name="processes">全工程のリスト</param>
        /// <param name="details">全工程詳細のリスト</param>
        /// <param name="operations">全操作のリスト</param>
        /// <param name="cylinders">全CYのリスト</param>
        /// <param name="ioList">全IOのリスト</param>
        public BuildDetailFunctions(
            // --- このクラスで直接使用する引数 ---
            MnemonicDeviceWithProcessDetail detail,
            MnemonicDeviceWithProcess process,
            // --- 基底クラスに渡すための引数 ---
            MainViewModel mainViewModel,
            IIOAddressService ioAddressService,
            IErrorAggregator errorAggregator,
            IAccessRepository repository,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList)
            // --- 基底クラスのコンストラクタを正しく呼び出す ---
            : base(mainViewModel, ioAddressService, errorAggregator, repository,
                   processes, details, operations, cylinders, ioList)
        {
            // このクラス固有のフィールドを初期化
            _detail = detail;
            _process = process;
            _label = detail.Mnemonic.DeviceLabel ?? string.Empty;
            _outNum = detail.Mnemonic.StartNum;

            // _details や _ioList の初期化は不要（基底クラスのコンストラクタが実行するため）
        }

        /// <summary>
        /// L0 工程開始のLadderCsvRowを生成します。
        /// </summary>
        /// <returns></returns>
        public List<LadderCsvRow> L0()
        {
            List<LadderCsvRow> result = new();

            var processDetailStartIds = _detail.Detail.StartIds?.Split(';')
                .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList() ?? new List<int>();
            var processDetailStartDevices = _details
                .Where(d => processDetailStartIds.Contains(d.Mnemonic.RecordId))
                .ToList();

            var processDeviceStartNum = _process?.Mnemonic.StartNum ?? 0;
            var processDeviceLabel = _process?.Mnemonic.DeviceLabel ?? string.Empty;

            // L0 工程開始
            // StartSensorが設定されている場合は、IOリストからセンサーを取得
            if (_detail.Detail.StartSensor != null)
            { 
                var ioSensor = _ioAddressService.GetSingleAddress(
                    _ioList,
                    _detail.Detail.StartSensor,
                    false,
                    _detail.Detail.DetailName!,
                    _detail.Detail.Id,
                    null);

                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.AlwaysOFF));
                }
                else
                {
                    if (_detail.Detail.StartSensor.Contains("_"))    // Containsではなく、先頭一文字
                    {
                        result.Add(LadderRow.AddLDI(ioSensor));
                        result.Add(LadderRow.AddOR(_label + (_outNum + 3).ToString()));
                    }
                    else
                    {
                        result.Add(LadderRow.AddLD(ioSensor));
                        result.Add(LadderRow.AddOR(_label + (_outNum + 3).ToString()));
                    }
                }
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));

            }
            else
            {
                result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));

            }
            result.Add(LadderRow.AddOR(_label + (_outNum + 0).ToString()));

            // 複数工程かどうか
            int blockNumber = _detail.Detail.BlockNumber ?? 0;
            if (blockNumber != 0)
            {
                var moduleDetail = _details.FirstOrDefault(d => d.Detail.Id == blockNumber);

                if (moduleDetail != null)
                {
                    processDeviceStartNum = moduleDetail.Mnemonic.StartNum;
                    processDeviceLabel = moduleDetail.Mnemonic.DeviceLabel ?? string.Empty;
                }
                else
                {
                    DetailError($"BlockNumber {blockNumber} に対応する工程詳細が見つかりません。");
                    return result; // エラーがある場合は、空のリストを返す
                }

            }
            result.Add(LadderRow.AddLD(processDeviceLabel + (processDeviceStartNum + 0).ToString()));

            foreach (var d in processDetailStartDevices)
            {
                if (_detail.Detail.StartSensor != null)
                {
                    result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 1).ToString()));
                }
                else
                {
                    result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 4).ToString()));
                }
            }

            result.Add(LadderRow.AddOR(_label + (_outNum + 3).ToString()));
            result.Add(LadderRow.AddANB());
            result.Add(LadderRow.AddOUT(_label + (_outNum + 0).ToString()));
            
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<MnemonicDeviceWithProcessDetail> StartDevices()
        {
            var processDetailStartIds = _detail.Detail.StartIds?.Split(';')
                .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList() ?? new List<int>();
            var processDetailStartDevices = _details
                .Where(d => processDetailStartIds.Contains(d.Mnemonic.RecordId))
                .ToList();
            return processDetailStartDevices;
        }

        public List<MnemonicDeviceWithProcessDetail> FinishDevices()
        {
            var processDetailFinishIds = _detail.Detail.FinishIds?.Split(';')
                .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList() ?? new List<int>();
            var processDetailFinishDevices = _details
                .Where(d => processDetailFinishIds.Contains(d.Mnemonic.RecordId))
                .ToList();
            return processDetailFinishDevices;
        }


        public void DetailError(string message)
        {
            // エラーをアグリゲートするメソッドを呼び出す
            _errorAggregator.AddError(new OutputError
            {
                Message = message,
                RecordName = _detail.Detail.DetailName,
                MnemonicId = (int)MnemonicType.ProcessDetail,
                RecordId = _detail.Detail.Id,
                IsCritical = true
            });
        }

    }
}
