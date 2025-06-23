using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services;
using KdxDesigner.Services.Error;
using KdxDesigner.Utils.MnemonicCommon;
using KdxDesigner.ViewModels;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Utils.Operation
{
    internal class IODebug
    {
        private readonly MnemonicDeviceWithOperation _operation;
        private readonly List<IO> _ioList;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;
        private readonly string _label;
        private readonly int _outNum;
        private readonly List<MnemonicDeviceWithCylinder> _cylinders;
        private readonly MainViewModel _mainViewModel;

        public IODebug(
            MainViewModel mainViewModel,
            MnemonicDeviceWithOperation operation,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            IErrorAggregator errorAggregator,
            IIOAddressService ioAddressService)
        {
            _mainViewModel = mainViewModel;
            _ioList = ioList;
            _cylinders = cylinders;
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
            _operation = operation;
            _label = operation.Mnemonic.DeviceLabel!; // ラベルの取得
            _outNum = operation.Mnemonic.StartNum; // ラベルの取得
        }

        public List<LadderCsvRow> GenerateCommon(int speedCount)
        {
            var result = new List<LadderCsvRow>();
            result.Add(LadderRow.AddLDP(SettingsManager.Settings.DebugPulse));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(_label + (_outNum + 6).ToString()));
            result.Add(LadderRow.AddANI(_label + (_outNum + 18).ToString()));

            // Valve1の処理
            if (_operation.Operation.Valve1 != null)
            {
                var selectedPlcId = _mainViewModel.SelectedPlc?.Id;
                if (selectedPlcId == null)
                {
                    throw new InvalidOperationException("Selected PLC is null.");
                }

                var valve1Address = _ioAddressService.GetSingleAddress(
                    _ioList,
                    _operation.Operation.Valve1,
                    selectedPlcId.Value);

                if (valve1Address != null)
                {
                    result.Add(LadderRow.AddAND(valve1Address));
                }
            }


            // Startセンサーの処理
            if (_operation.Operation.Start != null)
            {
                var selectedPlcId = _mainViewModel.SelectedPlc?.Id;
                if (selectedPlcId == null)
                {
                    throw new InvalidOperationException("Selected PLC is null.");
                }
                var startSensorAddress = _ioAddressService.GetSingleAddress(
                    _ioList,
                    _operation.Operation.Start,
                    selectedPlcId.Value);
                if (startSensorAddress != null)
                {
                    result.Add(LadderRow.AddRST(startSensorAddress));
                }

                // 速度処理
                // SS1の処理
                if (_operation.Operation.SS1 != null)
                {
                    var ss1Address = _ioAddressService.GetSingleAddress(
                        _ioList,
                        _operation.Operation.SS1,
                        selectedPlcId.Value);
                    if (ss1Address != null)
                    {
                        result.Add(LadderRow.AddMPS());
                        result.Add(LadderRow.AddAND(_label + (_outNum + 7).ToString()));
                        result.Add(LadderRow.AddANI(_label + (_outNum + 10).ToString()));
                        result.Add(LadderRow.AddSET(ss1Address));

                        result.Add(LadderRow.AddMPS());
                        result.Add(LadderRow.AddAND(_label + (_outNum + 10).ToString()));
                        result.Add(LadderRow.AddANI(_label + (_outNum + 17).ToString()));
                        result.Add(LadderRow.AddRST(ss1Address));
                    }

                    // SS2の処理
                    if (_operation.Operation.SS2 != null)
                    {
                        var ss2Address = _ioAddressService.GetSingleAddress(
                            _ioList,
                            _operation.Operation.SS2,
                            selectedPlcId.Value);
                        if (ss2Address != null)
                        {
                            result.Add(LadderRow.AddMRD());
                            result.Add(LadderRow.AddAND(_label + (_outNum + 10).ToString()));
                            result.Add(LadderRow.AddANI(_label + (_outNum + 11).ToString()));
                            result.Add(LadderRow.AddSET(ss2Address));
                            result.Add(LadderRow.AddMRD());
                            result.Add(LadderRow.AddAND(_label + (_outNum + 11).ToString()));
                            result.Add(LadderRow.AddANI(_label + (_outNum + 17).ToString()));
                            result.Add(LadderRow.AddRST(ss2Address));
                        }
                        // SS3の処理
                        if (_operation.Operation.SS3 != null)
                        {
                            var ss3Address = _ioAddressService.GetSingleAddress(
                                _ioList,
                                _operation.Operation.SS3,
                                selectedPlcId.Value);
                            if (ss3Address != null)
                            {
                                result.Add(LadderRow.AddMRD());
                                result.Add(LadderRow.AddAND(_label + (_outNum + 11).ToString()));
                                result.Add(LadderRow.AddANI(_label + (_outNum + 12).ToString()));
                                result.Add(LadderRow.AddSET(ss3Address));
                                result.Add(LadderRow.AddMRD());
                                result.Add(LadderRow.AddAND(_label + (_outNum + 12).ToString()));
                                result.Add(LadderRow.AddANI(_label + (_outNum + 17).ToString()));
                                result.Add(LadderRow.AddRST(ss3Address));
                            }
                            // SS4の処理
                            if (_operation.Operation.SS4 != null)
                            {
                                var ss4Address = _ioAddressService.GetSingleAddress(
                                    _ioList,
                                    _operation.Operation.SS4,
                                    selectedPlcId.Value);
                                if (ss4Address != null)
                                {
                                    result.Add(LadderRow.AddMRD());
                                    result.Add(LadderRow.AddAND(_label + (_outNum + 12).ToString()));
                                    result.Add(LadderRow.AddANI(_label + (_outNum + 13).ToString()));
                                    result.Add(LadderRow.AddSET(ss4Address));
                                    result.Add(LadderRow.AddMRD());
                                    result.Add(LadderRow.AddAND(_label + (_outNum + 13).ToString()));
                                    result.Add(LadderRow.AddANI(_label + (_outNum + 17).ToString()));
                                    result.Add(LadderRow.AddRST(ss4Address));
                                }
                            }
                        }
                    }
                }
                if (_operation.Operation.Finish != null)
                {
                    var finishSensorAddress = _ioAddressService.GetSingleAddress(
                        _ioList,
                        _operation.Operation.Finish,
                        selectedPlcId.Value);
                    if (finishSensorAddress != null)
                    {
                        switch (speedCount)
                        {
                            case 0:
                                result.Add(LadderRow.AddAND(_label + (_outNum + 7).ToString()));
                                result.Add(LadderRow.AddANI(_label + (_outNum + 17).ToString()));
                                result.Add(LadderRow.AddSET(finishSensorAddress));
                                break;
                            case 1:
                                result.Add(LadderRow.AddMPP());
                                result.Add(LadderRow.AddAND(_label + (_outNum + 10).ToString()));
                                result.Add(LadderRow.AddANI(_label + (_outNum + 17).ToString()));
                                result.Add(LadderRow.AddSET(finishSensorAddress));
                                break;
                            case 2:
                                result.Add(LadderRow.AddMPP());
                                result.Add(LadderRow.AddAND(_label + (_outNum + 11).ToString()));
                                result.Add(LadderRow.AddANI(_label + (_outNum + 17).ToString()));
                                result.Add(LadderRow.AddSET(finishSensorAddress));
                                break;
                            case 3:
                                result.Add(LadderRow.AddMPP());
                                result.Add(LadderRow.AddAND(_label + (_outNum + 12).ToString()));
                                result.Add(LadderRow.AddANI(_label + (_outNum + 17).ToString()));
                                result.Add(LadderRow.AddSET(finishSensorAddress));
                                break;
                            case 4:
                                result.Add(LadderRow.AddMPP());
                                result.Add(LadderRow.AddAND(_label + (_outNum + 13).ToString()));
                                result.Add(LadderRow.AddANI(_label + (_outNum + 17).ToString()));
                                result.Add(LadderRow.AddSET(finishSensorAddress));
                                break;
                            default:
                                throw new InvalidOperationException("Invalid speed operation.");
                        }
                    }
                }
            }
            else
            {
                // Finishセンサーの処理
                if (_operation.Operation.Finish != null)
                {
                    var selectedPlcId = _mainViewModel.SelectedPlc?.Id;
                    if (selectedPlcId == null)
                    {
                        throw new InvalidOperationException("Selected PLC is null.");
                    }
                    var finishSensorAddress = _ioAddressService.GetSingleAddress(
                        _ioList,
                        _operation.Operation.Finish,
                        selectedPlcId.Value);
                    if (finishSensorAddress != null)
                    {

                        result.Add(LadderRow.AddSET(finishSensorAddress));
                    }
                }
            }

            return result; // 生成されたLadderCsvRowのリストを返す

        }

    }
}
