﻿using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services.Error;
using KdxDesigner.Services.IOAddress;
using KdxDesigner.Utils.MnemonicCommon;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Utils.Cylinder
{
    internal class BuildCylinderSpeed
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;
        public BuildCylinderSpeed(MainViewModel mainViewModel, IErrorAggregator errorAggregator, IIOAddressService ioAddressService)
        {
            _mainViewModel = mainViewModel;
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
        }

        public List<LadderCsvRow> Flow1(
                MnemonicDeviceWithCylinder cylinder,
                List<MnemonicDeviceWithProcessDetail> details,
                List<MnemonicDeviceWithOperation> operations,
                List<MnemonicDeviceWithCylinder> cylinders,
                List<MnemonicTimerDeviceWithOperation> timers,
                List<MnemonicSpeedDevice> speed,
                List<Error> mnemonicError,
                List<ProsTime> prosTimes,
                List<IO> ioList)
        {
            // ここに単一工程の処理を実装  
            var result = new List<LadderCsvRow>();

            var cySpeedDevice = speed.Where(s => s.CylinderId == cylinder.Cylinder.Id).SingleOrDefault(); // スピードデバイスの取得
            string? speedDevice;
            if (cySpeedDevice == null)
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = cylinder.Cylinder.Id,
                    RecordName = cylinder.Cylinder.CYNum,
                    Message = $"CY{cylinder.Cylinder.CYNum}のスピードデバイスが見つかりません。",
                });
                speedDevice = null; // スピードデバイスが見つからない場合はnullを設定
            }
            else
            {
                speedDevice = cySpeedDevice.Device; // スピードデバイスの取得
            }

            var functions = new CylinderFunction(_mainViewModel, _errorAggregator, cylinder, _ioAddressService, speedDevice);

            // CYNumを含むIOの取得
            var sensors = ioList.Where(i => i.IOName != null
                                            && cylinder.Cylinder.CYNum != null
                                            && i.IOName.Contains(cylinder.Cylinder.CYNum)).ToList();

            // 行間ステートメント  
            string id = cylinder.Cylinder.Id.ToString();
            string cyNum = cylinder.Cylinder.CYNum ?? ""; // シリンダー名の取得  
            string cyNumSub = cylinder.Cylinder.CYNameSub.ToString() ?? ""; // シリンダー名の取得  
            string cyName = cyNum + cyNumSub; // シリンダー名の組み合わせ  

            result.Add(LadderRow.AddStatement(id + ":" + cyName + "比例流量弁"));

            var label = cylinder.Mnemonic.DeviceLabel; // ラベルの取得  
            var startNum = cylinder.Mnemonic.StartNum; // ラベルの取得  

            // CYが一致するOperationの取得  
            var cylinderOperations = operations.Where(o => o.Operation.CYId == cylinder.Cylinder.Id).ToList();
            var goOperation = cylinderOperations.Where(o => o.Operation.GoBack == "G").ToList();        // 行きのOperationを取得  
            var backOperation = cylinderOperations.Where(o => o.Operation.GoBack == "B").ToList();      // 帰りのOperationを取得  
            var activeOperation = cylinderOperations.Where(o => o.Operation.GoBack == "A").ToList();    // 作動のOperationを取得  

            // 行き方向自動指令  
            if (goOperation.Count != 0 && activeOperation.Count == 0)
            {
                result.AddRange(functions.GoOperation(goOperation));
                // 帰り方向自動指令
                result.AddRange(functions.BackOperation(backOperation));
                result.AddRange(functions.GoManualOperation(goOperation));
                result.AddRange(functions.BackManualOperation(backOperation));

            }
            // 行き方向自動指令がない場合は、行き方向手動指令を使用
            else if (goOperation.Count == 0 && activeOperation.Count != 0)
            {

                result.AddRange(functions.GoOperation(activeOperation));
                // 帰り方向自動指令
                result.AddRange(functions.BackOperation(backOperation));
                result.AddRange(functions.GoManualOperation(activeOperation));
                result.AddRange(functions.BackManualOperation(backOperation));

            }

            result.AddRange(functions.ManualReset());

            result.Add(LadderRow.AddNOP());

            // Cycleスタート時の方向自動指令
            result.AddRange(functions.CyclePulse());

            // 保持
            result.AddRange(functions.OutputRetention());

            // 保持出力
            result.AddRange(functions.RetentionFlow(sensors));
            result.Add(LadderRow.AddNOP());

            // マニュアル
            result.AddRange(functions.ManualButton());
            result.Add(LadderRow.AddNOP());


            // 速度指令
            result.AddRange(functions.FlowOperate());
            result.Add(LadderRow.AddNOP());

            // 出力OK
            result.AddRange(functions.ILOK());
            result.Add(LadderRow.AddNOP());

            // バルブ指令
            if (speedDevice != null)
            {
                if (cylinder.Cylinder.FlowCount != null)
                {
                    for (int i = 1; i <= cylinder.Cylinder.FlowCount; i++)
                    {
                        string countFlowName = cyName + i.ToString();
                        var flowSensors = sensors.Where(i => i.IOName!.Contains(countFlowName)).ToList();
                        result.AddRange(functions.FlowValve(flowSensors, speedDevice));
                    }
                }
                else
                {
                    result.AddRange(functions.FlowValve(sensors, speedDevice));
                }
            }

            return result;
        }

        public List<LadderCsvRow> Inverter(
                MnemonicDeviceWithCylinder cylinder,
                List<MnemonicDeviceWithProcessDetail> details,
                List<MnemonicDeviceWithOperation> operations,
                List<MnemonicDeviceWithCylinder> cylinders,
                List<MnemonicTimerDeviceWithOperation> timers,
                List<MnemonicTimerDeviceWithCylinder> cylinderTimers,
                List<MnemonicSpeedDevice> speed,
                List<Error> mnemonicError,
                List<ProsTime> prosTimes,
                List<IO> ioList)
        {
            // ここに単一工程の処理を実装  
            var result = new List<LadderCsvRow>();

            var cySpeedDevice = speed.Where(s => s.CylinderId == cylinder.Cylinder.Id).SingleOrDefault(); // スピードデバイスの取得
            string? speedDevice;
            if (cySpeedDevice == null)
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = cylinder.Cylinder.Id,
                    RecordName = cylinder.Cylinder.CYNum,
                    Message = $"CY{cylinder.Cylinder.CYNum}のスピードデバイスが見つかりません。",
                });
                speedDevice = null; // スピードデバイスが見つからない場合はnullを設定
            }
            else
            {
                speedDevice = cySpeedDevice.Device; // スピードデバイスの取得
            }

            var functions = new CylinderFunction(_mainViewModel, _errorAggregator, cylinder, _ioAddressService, speedDevice);

            // CYNumを含むIOの取得
            var sensors = ioList.Where(i => i.IOName != null
                                            && cylinder.Cylinder.CYNum != null
                                            && i.IOName.Contains(cylinder.Cylinder.CYNum)).ToList();

            // 行間ステートメント  
            string id = cylinder.Cylinder.Id.ToString();
            string cyNum = cylinder.Cylinder.CYNum ?? ""; // シリンダー名の取得  
            string cyNumSub = cylinder.Cylinder.CYNameSub.ToString() ?? ""; // シリンダー名の取得  
            string cyName = cyNum + cyNumSub; // シリンダー名の組み合わせ  

            result.Add(LadderRow.AddStatement(id + ":" + cyName + " インバータ"));

            var label = cylinder.Mnemonic.DeviceLabel; // ラベルの取得  
            var startNum = cylinder.Mnemonic.StartNum; // ラベルの取得  

            // CYが一致するOperationの取得  
            var cylinderOperations = operations.Where(o => o.Operation.CYId == cylinder.Cylinder.Id).ToList();
            var goOperation = cylinderOperations.Where(o => o.Operation.GoBack == "G").ToList();        // 行きのOperationを取得  
            var backOperation = cylinderOperations.Where(o => o.Operation.GoBack == "B").ToList();      // 帰りのOperationを取得  
            var activeOperation = cylinderOperations.Where(o => o.Operation.GoBack == "A").ToList();    // 作動のOperationを取得  

            // 行き方向自動指令  
            if (goOperation.Count != 0 && activeOperation.Count == 0)
            {
                result.AddRange(functions.GoOperation(goOperation));
                // 帰り方向自動指令
                result.AddRange(functions.BackOperation(backOperation));
                result.AddRange(functions.GoManualOperation(goOperation));
                result.AddRange(functions.BackManualOperation(backOperation));

            }
            // 行き方向自動指令がない場合は、行き方向手動指令を使用
            else if (goOperation.Count == 0 && activeOperation.Count != 0)
            {

                result.AddRange(functions.GoOperation(activeOperation));
                // 帰り方向自動指令
                result.AddRange(functions.BackOperation(backOperation));
                result.AddRange(functions.GoManualOperation(activeOperation));
                result.AddRange(functions.BackManualOperation(backOperation));

            }

            result.AddRange(functions.ManualReset());

            result.Add(LadderRow.AddNOP());

            // マニュアル
            result.AddRange(functions.ManualButton());
            result.Add(LadderRow.AddNOP());

            // 行き方向自動
            result.Add(LadderRow.AddLD(label + (startNum + 0).ToString()));
            result.Add(LadderRow.AddOR(label + (startNum + 2).ToString()));
            result.Add(LadderRow.AddOUT(label + (startNum + 12).ToString()));

            // 帰り方向自動
            result.Add(LadderRow.AddLD(label + (startNum + 1).ToString()));
            result.Add(LadderRow.AddOR(label + (startNum + 3).ToString()));
            result.Add(LadderRow.AddOUT(label + (startNum + 13).ToString()));

            // 指令ON
            result.Add(LadderRow.AddLD(label + (startNum + 12).ToString()));
            result.Add(LadderRow.AddOR(label + (startNum + 13).ToString()));
            result.Add(LadderRow.AddLD(label + (startNum + 7).ToString()));
            result.Add(LadderRow.AddAND(cylinder.Cylinder.ManualButton));
            result.Add(LadderRow.AddORB());
            result.Add(LadderRow.AddLD(label + (startNum + 8).ToString()));
            result.Add(LadderRow.AddAND(cylinder.Cylinder.ManualButton));
            result.Add(LadderRow.AddORB());
            result.Add(LadderRow.AddOUT(label + (startNum + 10).ToString()));
            result.Add(LadderRow.AddNOP());

            // 強制減速・手動操作
            var timerList = cylinderTimers.Where(t => t.Timer.RecordId == cylinder.Cylinder.Id).ToList();
            var fltTimer = timerList.FirstOrDefault(t => t.Timer.TimerCategoryId == 14); // 強制減速タイマ
            var ebtTimer = timerList.FirstOrDefault(t => t.Timer.TimerCategoryId == 6); // 異常時BKタイマ
            var nbtTimer = timerList.FirstOrDefault(t => t.Timer.TimerCategoryId == 7); // 正常時BKタイマ

            if (fltTimer == null)
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = cylinder.Cylinder.Id,
                    RecordName = cylinder.Cylinder.CYNum,
                    Message = $"CY{cylinder.Cylinder.CYNum}の強制減速タイマが見つかりません。",
                });
            }
            else
            {
                if (speedDevice != null)
                {
                    // 強制減速タイマがある場合の処理
                    result.AddRange(LadderRow.AddLDN(speedDevice, "K1"));
                    result.AddRange(LadderRow.AddANDN(speedDevice, "K5"));
                    result.AddRange(LadderRow.AddTimer(fltTimer.Timer.ProcessTimerDevice, fltTimer.Timer.TimerDevice));
                    result.Add(LadderRow.AddLD(fltTimer.Timer.ProcessTimerDevice));
                    result.Add(LadderRow.AddMPS());
                    result.Add(LadderRow.AddAND(label + (startNum + 35).ToString()));
                    result.AddRange(LadderRow.AddMOVSet("K1", speedDevice));
                    result.Add(LadderRow.AddMPP());
                    result.Add(LadderRow.AddAND(label + (startNum + 36).ToString()));
                    result.AddRange(LadderRow.AddMOVSet("K5", speedDevice));

                    // 手動JOG指令 行き
                    result.Add(LadderRow.AddLD(label + (startNum + 7).ToString()));
                    result.Add(LadderRow.AddAND(cylinder.Cylinder.ManualButton));
                    result.AddRange(LadderRow.AddMOVSet("K1", speedDevice));

                    // 手動JOG指令 帰り
                    result.Add(LadderRow.AddLD(label + (startNum + 8).ToString()));
                    result.Add(LadderRow.AddAND(cylinder.Cylinder.ManualButton));
                    result.AddRange(LadderRow.AddMOVSet("K5", speedDevice));
                }
                else
                {
                    _errorAggregator.AddError(new OutputError
                    {
                        MnemonicId = (int)MnemonicType.CY,
                        RecordId = cylinder.Cylinder.Id,
                        RecordName = cylinder.Cylinder.CYNum,
                        Message = $"CY{cylinder.Cylinder.CYNum}のスピードデバイスが見つかりません。",
                    });
                }
            }

            result.Add(LadderRow.AddNOP());


            // 速度指令
            if (speedDevice != null)
            {
                result.Add(LadderRow.AddLD(label + (startNum + 10).ToString()));
                result.Add(LadderRow.AddAND(cylinder.Cylinder.ManualButton));
                result.Add(LadderRow.AddAND(label + (startNum + 12).ToString()));
                result.Add(LadderRow.AddAND(label + (startNum + 13).ToString()));
                result.AddRange(LadderRow.AddMOVSet("K1", speedDevice));

                for (int i = 1; i < 11; i++)
                {
                    result.AddRange(LadderRow.AddLDE(speedDevice, "K" + i.ToString()));
                    result.Add(LadderRow.AddAND(label + (startNum + 10).ToString()));
                    result.Add(LadderRow.AddOUT(label + (startNum + 20 + i).ToString()));
                }

            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = cylinder.Cylinder.Id,
                    RecordName = cylinder.Cylinder.CYNum,
                    Message = $"CY{cylinder.Cylinder.CYNum}のスピードデバイスが見つかりません。",
                });
            }
            result.Add(LadderRow.AddNOP());

            // 出力OK
            result.AddRange(functions.ILOK());
            result.Add(LadderRow.AddNOP());


            if (speedDevice != null)
            {
                // 高速停止記憶
                result.Add(LadderRow.AddLDI(label + (startNum + 35).ToString()));
                result.Add(LadderRow.AddANI(label + (startNum + 36).ToString()));
                result.Add(LadderRow.AddMEP());
                result.AddRange(LadderRow.AddANDN(speedDevice, "K1"));
                result.AddRange(LadderRow.AddANDN(speedDevice, "K5"));
                result.Add(LadderRow.AddSET(label + (startNum + 33).ToString()));

                // 高速停止記憶解除
                if (ebtTimer != null)
                {

                    result.Add(LadderRow.AddLDI(label + (startNum + 35).ToString()));
                    result.Add(LadderRow.AddANI(label + (startNum + 36).ToString()));
                    result.Add(LadderRow.AddMPS());
                    result.Add(LadderRow.AddANI(ebtTimer.Timer.ProcessTimerDevice));
                    result.Add(LadderRow.AddANI(label + (startNum + 33).ToString()));
                    result.Add(LadderRow.AddOUT(label + (startNum + 34).ToString()));
                    result.Add(LadderRow.AddMPP());
                    result.AddRange(LadderRow.AddTimer(ebtTimer.Timer.ProcessTimerDevice, ebtTimer.Timer.TimerDevice));
                }
                else
                {
                    _errorAggregator.AddError(new OutputError
                    {
                        MnemonicId = (int)MnemonicType.CY,
                        RecordId = cylinder.Cylinder.Id,
                        RecordName = cylinder.Cylinder.CYNum,
                        Message = $"CY{cylinder.Cylinder.CYNum}の異常時BKタイマが見つかりません。",
                    });
                }

            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = cylinder.Cylinder.Id,
                    RecordName = cylinder.Cylinder.CYNum,
                    Message = $"CY{cylinder.Cylinder.CYNum}のスピードデバイスが見つかりません。",
                });
            }

            // ﾌﾞﾚｰｷ接点の検索
            // ブレーキ接点の検索
            var breakeList = sensors.Where(i => i.IOName != null
                                           && i.IOName.Contains(cylinder.Cylinder.CYNum ?? string.Empty)
                                           && !i.IOName.Contains("STF")
                                           && !i.IOName.Contains("STR")).ToList();

            var breakeIO = _ioAddressService.
                GetSingleAddress(
                breakeList,
                cylinder.Cylinder.CYNum + "S",
                true,
                cylinder.Cylinder.CYNum!,
                recordId: cylinder.Cylinder.Id,
                null);

            if (nbtTimer != null)
            {
                if (breakeIO != null)
                {
                    // 正常時BKタイマ
                    result.Add(LadderRow.AddLD(label + (startNum + 35).ToString()));
                    result.Add(LadderRow.AddOR(label + (startNum + 36).ToString()));
                    result.Add(LadderRow.AddOR(label + (startNum + 34).ToString()));
                    result.Add(LadderRow.AddOUT(label + (startNum + 36).ToString()));
                    result.Add(LadderRow.AddOUT(breakeIO));
                    result.AddRange(LadderRow.AddTimer(nbtTimer.Timer.ProcessTimerDevice, nbtTimer.Timer.TimerDevice));

                    // 指令OK
                    result.Add(LadderRow.AddLD(label + (startNum + 35).ToString()));
                    result.Add(LadderRow.AddOR(label + (startNum + 36).ToString()));
                    result.Add(LadderRow.AddAND(nbtTimer.Timer.ProcessTimerDevice));
                    result.Add(LadderRow.AddOUT(label + (startNum + 37).ToString()));
                    result.Add(LadderRow.AddNOP());
                }
                else
                {
                    _errorAggregator.AddError(new OutputError
                    {
                        MnemonicId = (int)MnemonicType.CY,
                        RecordId = cylinder.Cylinder.Id,
                        RecordName = cylinder.Cylinder.CYNum,
                        Message = $"CY{cylinder.Cylinder.CYNum}のブレーキIOが見つかりません。",
                    });
                }
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = cylinder.Cylinder.Id,
                    RecordName = cylinder.Cylinder.CYNum,
                    Message = $"CY{cylinder.Cylinder.CYNum}の正常時BKタイマが見つかりません。",
                });
            }

            // Fix for CS8604: Ensure 'recordName' is not null before passing it to 'GetSingleAddress'.
            var stfIO = _ioAddressService.GetSingleAddress(
                sensors,
                "STF",
                true,
                cylinder.Cylinder.CYNum ?? string.Empty, // Use null-coalescing operator to provide a default value
                cylinder.Cylinder.Id,
                null);
            if (stfIO != null)
            {
                result.Add(LadderRow.AddLD(label + (startNum + 35).ToString()));
                result.Add(LadderRow.AddAND(label + (startNum + 37).ToString()));
                result.Add(LadderRow.AddOUT(stfIO));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = cylinder.Cylinder.Id,
                    RecordName = cylinder.Cylinder.CYNum,
                    Message = $"CY{cylinder.Cylinder.CYNum}の正転指令IOが見つかりません。",
                });
            }

            // 逆転指令
            var strIO = _ioAddressService.GetSingleAddress(
                                        sensors,
                                        "STR",
                                        true,
                                        (cylinder.Cylinder.CYNum + cyName) ?? string.Empty, // Ensure concatenated string is not null
                                        cylinder.Cylinder.Id,
                                        null);
            if (strIO != null)
            {
                result.Add(LadderRow.AddLD(label + (startNum + 35).ToString()));
                result.Add(LadderRow.AddAND(label + (startNum + 37).ToString()));
                result.Add(LadderRow.AddOUT(strIO));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = cylinder.Cylinder.Id,
                    RecordName = cylinder.Cylinder.CYNum,
                    Message = $"CY{cylinder.Cylinder.CYNum}の逆転指令IOが見つかりません。",
                });
            }

            // 逆転指令
            var rlIO = _ioAddressService.GetSingleAddress(sensors, "RL", true, cyNum + cyName, cylinder.Cylinder.Id, null);
            var rmIO = _ioAddressService.GetSingleAddress(sensors, "RM", true, cyNum + cyName, cylinder.Cylinder.Id, null);
            var rhIO = _ioAddressService.GetSingleAddress(sensors, "RH", true, cyNum + cyName, cylinder.Cylinder.Id, null);

            if (rlIO != null && rmIO != null && rhIO != null)
            {
                // RL
                result.Add(LadderRow.AddLD(label + (startNum + 23).ToString()));
                result.Add(LadderRow.AddOR(label + (startNum + 24).ToString()));
                result.Add(LadderRow.AddOR(label + (startNum + 25).ToString()));
                result.Add(LadderRow.AddOR(label + (startNum + 27).ToString()));
                result.Add(LadderRow.AddAND(label + (startNum + 37).ToString()));
                result.Add(LadderRow.AddOUT(rlIO));

                // RM
                result.Add(LadderRow.AddLD(label + (startNum + 22).ToString()));
                result.Add(LadderRow.AddOR(label + (startNum + 24).ToString()));
                result.Add(LadderRow.AddOR(label + (startNum + 26).ToString()));
                result.Add(LadderRow.AddOR(label + (startNum + 27).ToString()));
                result.Add(LadderRow.AddAND(label + (startNum + 37).ToString()));
                result.Add(LadderRow.AddOUT(rmIO));

                // RH
                result.Add(LadderRow.AddLD(label + (startNum + 21).ToString()));
                result.Add(LadderRow.AddOR(label + (startNum + 25).ToString()));
                result.Add(LadderRow.AddOR(label + (startNum + 26).ToString()));
                result.Add(LadderRow.AddOR(label + (startNum + 27).ToString()));
                result.Add(LadderRow.AddAND(label + (startNum + 37).ToString()));
                result.Add(LadderRow.AddOUT(rhIO));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = cylinder.Cylinder.Id,
                    RecordName = cylinder.Cylinder.CYNum,
                    Message = $"CY{cylinder.Cylinder.CYNum}の速度指令接点(RL, RM, RH)が見つかりません。",
                });
            }
            return result;
        }
    }
}