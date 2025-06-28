using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services.IOAddress;
using KdxDesigner.Services.Error;
using KdxDesigner.Utils.MnemonicCommon;
using KdxDesigner.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;

namespace KdxDesigner.Utils.Operation
{
    /// <summary>
    /// OperationのIOデバッグに関連するラダーロジックを生成します。
    /// </summary>
    internal class IODebug
    {
        private readonly MainViewModel _mainViewModel;
        private readonly MnemonicDeviceWithOperation _operation;
        private readonly List<IO> _ioList;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;
        private readonly string _label;
        private readonly int _outNum;
        private readonly List<MnemonicDeviceWithCylinder> _cylinders;

        public IODebug(
            MainViewModel mainViewModel,
            MnemonicDeviceWithOperation operation,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            IErrorAggregator errorAggregator,
            IIOAddressService ioAddressService)
        {
            _mainViewModel = mainViewModel;
            _operation = operation;
            _cylinders = cylinders;
            _ioList = ioList;
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;

            _label = operation.Mnemonic.DeviceLabel ?? "";
            _outNum = operation.Mnemonic.StartNum;
        }

        /// <summary>
        /// IOデバッグ用の共通ラダー回路を生成します。
        /// </summary>
        public List<LadderCsvRow> GenerateCommon(int speedCount)
        {
            var result = new List<LadderCsvRow>();

            // --- デバッグパルスによる基本条件 ---
            result.Add(LadderRow.AddLDP(SettingsManager.Settings.DebugPulse));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(_label + (_outNum + 6)));
            result.Add(LadderRow.AddANI(_label + (_outNum + 18)));

            // --- Valve1 の処理 ---
            if (!string.IsNullOrEmpty(_operation.Operation.Valve1) 
                && _operation.Operation.CategoryId != 20)
            {
                // GetSingleAddressの代わりにGetAddressRangeを使い、複数の候補を取得する
                var valveCandidates = _ioAddressService.GetAddressRange(
                    _ioList,
                    _operation.Operation.Valve1,
                    _operation.Operation.OperationName!,
                    _operation.Operation.Id,
                    errorIfNotFound: true); // 候補が1つも見つからない場合は従来通りエラーとする

                // 見つかった候補のリストから最初の1つのアドレスを取得する
                // 候補がない場合はnullになる
                var valve1Address = valveCandidates.FirstOrDefault()?.Address;

                if (valve1Address != null)
                {
                    result.Add(LadderRow.AddAND(valve1Address));
                }
            }

            // --- Startセンサーの処理 ---
            HandleStartSensor(result);

            // --- 速度(SS1-4) と Finishセンサーの処理 ---
            if (!string.IsNullOrEmpty(_operation.Operation.Start))
            {
                HandleSpeedAndFinishWithStart(result, speedCount);
            }
            else
            {
                HandleFinishWithoutStart(result);
            }

            return result;
        }

        /// <summary>
        /// StartセンサーのSET/RSTロジックを生成します。
        /// </summary>
        private void HandleStartSensor(List<LadderCsvRow> result)
        {
            if (string.IsNullOrEmpty(_operation.Operation.Start)) return;

            if (_operation.Operation.SC != null && _operation.Operation.SC != 0)
            {
                // SCあり: 複数センサーをRST
                // ★修正: GetAddressRange の引数を新しいI/Fに合わせる
                var ioSensors = _ioAddressService.GetAddressRange(
                    _ioList, 
                    _operation.Operation.Start,
                    _operation.Operation.OperationName!,
                    _operation.Operation.Id,
                    errorIfNotFound: true);
                foreach (var io in ioSensors)
                {
                    if (!string.IsNullOrEmpty(io.Address))
                    {
                        result.Add(LadderRow.AddRST(io.Address));
                    }
                }
            }
            else
            {
                // SCなし: 単一センサーをSET/RST
                // ★修正: GetSingleAddress の引数を新しいI/Fに合わせる
                var ioSensor = _ioAddressService.GetSingleAddress(
                    _ioList, 
                    _operation.Operation.Start,
                    false,
                    _operation.Operation.OperationName,
                    _operation.Operation.Id,
                    null);
                if (ioSensor == null)
                {
                    result.Add(LadderRow.AddAND(SettingsManager.Settings.AlwaysON));
                }
                else
                {
                    if (_operation.Operation.Start.StartsWith("_"))
                    {
                        result.Add(LadderRow.AddSET(ioSensor));
                    }
                    else
                    {
                        result.Add(LadderRow.AddRST(ioSensor));
                    }
                }
            }
        }

        /// <summary>
        /// Startセンサーがある場合の、速度信号(SS)とFinishセンサーのロジックを生成します。
        /// </summary>
        private void HandleSpeedAndFinishWithStart(List<LadderCsvRow> result, int speedCount)
        {
            result.Add(LadderRow.AddMPS());

            HandleSpeedSensor(result, _operation.Operation.SS1, 7, 10);
            HandleSpeedSensor(result, _operation.Operation.SS2, 10, 11);
            HandleSpeedSensor(result, _operation.Operation.SS3, 11, 12);
            HandleSpeedSensor(result, _operation.Operation.SS4, 12, 13);

            HandleFinishWithStart(result, speedCount);

            result.Add(LadderRow.AddMPP());
        }

        /// <summary>
        /// 速度センサー(SS)のSET/RSTロジックを生成するヘルパーメソッド。
        /// </summary>
        private void HandleSpeedSensor(List<LadderCsvRow> result, string? ssSignal, int andOffset, int aniOffset)
        {
            if (string.IsNullOrEmpty(ssSignal)) return;

            // ★修正: GetSingleAddress の引数を新しいI/Fに合わせる
            var ssAddress = _ioAddressService.GetSingleAddress(
                _ioList, 
                ssSignal,
                false,
                _operation.Operation.OperationName,
                _operation.Operation.Id,
                null);
            if (ssAddress == null) return;

            result.Add(LadderRow.AddMRD());
            result.Add(LadderRow.AddAND(_label + (_outNum + andOffset)));
            result.Add(LadderRow.AddANI(_label + (_outNum + aniOffset)));
            result.Add(LadderRow.AddSET(ssAddress));

            result.Add(LadderRow.AddMRD());
            result.Add(LadderRow.AddAND(_label + (_outNum + aniOffset)));
            result.Add(LadderRow.AddANI(_label + (_outNum + 17)));
            result.Add(LadderRow.AddRST(ssAddress));
        }


        /// <summary>
        /// Start信号がある場合のFinishセンサーをSETするためのラダー命令を生成します。
        /// </summary>
        private void HandleFinishWithStart(List<LadderCsvRow> result, int speedCount)
        {
            if (string.IsNullOrEmpty(_operation.Operation.Finish)) return;

            var conditionAndOffset = speedCount switch
            {
                0 => 7,
                1 => 10,
                2 => 11,
                3 => 12,
                4 => 13,
                _ => -1
            };

            if (conditionAndOffset == -1)
            {
                _errorAggregator.AddError(new OutputError { Message = $"無効な speedCount ({speedCount}) が指定されました。", RecordId = _operation.Operation.Id });
                return;
            }

            List<string> addressesToSet = new();
            if (_operation.Operation.FC != null && _operation.Operation.FC != 0)
            {
                // ★修正: GetAddressRange の引数を新しいI/Fに合わせる
                var finishSensors = _ioAddressService.GetAddressRange(
                    _ioList, 
                    _operation.Operation.Finish,
                    _operation.Operation.OperationName!,
                    _operation.Operation.Id,
                    errorIfNotFound: true);
                addressesToSet.AddRange(finishSensors.Select(s => s.Address).Where(a => !string.IsNullOrEmpty(a))!);
            }
            else
            {
                // ★修正: GetSingleAddress の引数を新しいI/Fに合わせる
                var addr = _ioAddressService.GetSingleAddress(
                    _ioList, 
                    _operation.Operation.Finish,
                    false,
                    _operation.Operation.OperationName!,
                    _operation.Operation.Id,
                    null);
                if (addr != null) addressesToSet.Add(addr);
            }

            if (addressesToSet.Any())
            {
                result.Add(LadderRow.AddMRD());
                result.Add(LadderRow.AddAND(_label + (_outNum + conditionAndOffset)));
                result.Add(LadderRow.AddANI(_label + (_outNum + 17)));

                foreach (var address in addressesToSet)
                {
                    result.Add(LadderRow.AddSET(address));
                }
            }
        }

        /// <summary>
        /// Startセンサーがない場合の、Finishセンサーのロジックを生成します。
        /// </summary>
        private void HandleFinishWithoutStart(List<LadderCsvRow> result)
        {
            if (string.IsNullOrEmpty(_operation.Operation.Finish)) return;

            if (_operation.Operation.FC != null && _operation.Operation.FC != 0)
            {
                // ★修正: GetAddressRange の引数を新しいI/Fに合わせる
                var finishSensors = _ioAddressService.GetAddressRange(
                    _ioList, 
                    _operation.Operation.Finish,
                    _operation.Operation.OperationName!,
                    _operation.Operation.Id,
                    errorIfNotFound: true);
                foreach (var sensor in finishSensors)
                {
                    if (!string.IsNullOrEmpty(sensor.Address))
                    {
                        result.Add(LadderRow.AddSET(sensor.Address));
                    }
                }
            }
            else
            {
                // ★修正: GetSingleAddress の引数を新しいI/Fに合わせる
                var finishSensorAddress = _ioAddressService.GetSingleAddress(
                    _ioList, 
                    _operation.Operation.Finish,
                    false,
                    _operation.Operation.OperationName!,
                    _operation.Operation.Id, 
                    null);
                if (finishSensorAddress != null)
                {
                    if (_operation.Operation.Finish.StartsWith("_"))
                    {
                        result.Add(LadderRow.AddANI(finishSensorAddress));
                    }
                    else
                    {
                        result.Add(LadderRow.AddAND(finishSensorAddress));
                    }
                }
                else
                {
                    result.Add(LadderRow.AddAND(SettingsManager.Settings.AlwaysON));
                }
            }
        }
    }
}