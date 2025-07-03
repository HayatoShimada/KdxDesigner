using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services;
using KdxDesigner.Services.Access;
using KdxDesigner.Services.Error;
using KdxDesigner.Services.IOAddress;
using KdxDesigner.Utils.MnemonicCommon;
using KdxDesigner.ViewModels;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Utils.ProcessDetail
{
    internal class BuildDetailFunctions : BuildDetail
    {
        private readonly MnemonicDeviceWithProcessDetail _detail;
        private readonly MnemonicDeviceWithProcess _process;
        private readonly List<MnemonicDeviceWithProcessDetail> _details;
        private readonly List<IO> _ioList;
        private readonly string _label;
        private readonly int _outNum;

        public BuildDetailFunctions(
            // --- このクラス固有の引数 ---
            MnemonicDeviceWithProcessDetail detail,
            MnemonicDeviceWithProcess process,
            List<MnemonicDeviceWithProcessDetail> details,
            List<IO> ioList,
            // --- 基底クラスに渡すための引数 ---
            MainViewModel mainViewModel,
            IIOAddressService ioAddressService, // 変更点1: 引数を追加
            IErrorAggregator errorAggregator,
            IAccessRepository repository,
            List<MnemonicDeviceWithProcess> processes)
            // 変更点2: 基底クラスのコンストラクタに合わせて引数の順序を修正
            : base(mainViewModel, ioAddressService, errorAggregator, repository, processes)
        {
            _detail = detail; // MnemonicDeviceWithProcessDetailのインスタンスを取得
            _process = process; // MnemonicDeviceWithProcessのインスタンスを取得
            _details = details; // MnemonicDeviceWithProcessDetailのリストを取得
            _ioList = ioList; // IOリストのインスタンスを取得
            _label = detail.Mnemonic.DeviceLabel ?? string.Empty; // ラベルを設定
            _outNum = detail.Mnemonic.StartNum; // 出力番号を設定
        }

        public List<LadderCsvRow> L0()
        {
            List<LadderCsvRow> result = new List<LadderCsvRow>();

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
                if (_detail.Detail.TimerId != null)
                {
                    DetailError("StartSensorが設定されている場合は、TimerIdを設定しないでください。");
                    return result; // エラーがある場合は、空のリストを返す
                }

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
                    }
                    else
                    {
                        result.Add(LadderRow.AddLD(ioSensor));
                    }
                }
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));

            }
            else
            {
                if (_detail.Detail.TimerId != null)
                {

                    MnemonicTimerDeviceService timerDeviceService = new MnemonicTimerDeviceService(_repository);
                    var timerDevice = timerDeviceService.GetMnemonicTimerDeviceByTimerId(_mainViewModel.SelectedPlc!.Id, _detail.Detail.TimerId.Value);
                    if (timerDevice == null)
                    {
                        DetailError($"TimerIdが設定されている場合は、StartSensorを設定しないでください。");
                        return result; // エラーがある場合は、空のリストを返す
                    }
                    else
                    {
                        // タイマーデバイスが取得できた場合は、LDコマンドを追加
                        result.Add(LadderRow.AddLD(timerDevice.ProcessTimerDevice));
                        result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
                    }

                }
                else
                {
                    result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));

                }

            }
            result.Add(LadderRow.AddOR(_label + (_outNum + 0).ToString()));
            result.Add(LadderRow.AddAND(processDeviceLabel + (processDeviceStartNum + 0).ToString()));

            foreach (var d in processDetailStartDevices)
            {
                result.Add(LadderRow.AddAND(d.Mnemonic.DeviceLabel + (d.Mnemonic.StartNum + 9).ToString()));
            }
            result.Add(LadderRow.AddOUT(_label + (_outNum + 0).ToString()));

            return result;
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
