using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Utils.MnemonicCommon;
using KdxDesigner.ViewModels;

using System.Diagnostics;
using System.Reflection.Emit;

namespace KdxDesigner.Utils.Cylinder
{
    internal class BuildCylinder
    {
        public static List<LadderCsvRow> Valve1(
                MnemonicDeviceWithCylinder cylinder,
                List<MnemonicDeviceWithProcessDetail> details,
                List<MnemonicDeviceWithOperation> operations,
                List<MnemonicDeviceWithCylinder> cylinders,
                List<MnemonicTimerDeviceWithOperation> timers,
                List<Error> mnemonicError,
                List<ProsTime> prosTimes,
                List<IO> ioList,
                out List<OutputError> errors,
                int plcId,
                MainViewModel mainViewModel)
        {
            // ここに単一工程の処理を実装  
            errors = new List<OutputError>(); // エラーリストの初期化  
            var result = new List<LadderCsvRow>(); // 生成されるLadderCsvRowのリスト  
            List<OutputError> localErrors = new();

            var cycles = mainViewModel.Cycles;

            // CYNumを含むIOの取得
            var sensors = ioList.Where(i => i.IOName != null
                                            && cylinder.Cylinder.CYNum != null
                                            && i.IOName.Contains(cylinder.Cylinder.CYNum)).ToList();

            // 行間ステートメント  
            string id = cylinder.Cylinder.Id.ToString();
            string cyNum = cylinder.Cylinder.CYNum ?? ""; // シリンダー名の取得  
            string cyNumSub = cylinder.Cylinder.CYNameSub.ToString() ?? ""; // シリンダー名の取得  
            string cyName = cyNum + cyNumSub; // シリンダー名の組み合わせ  

            result.Add(LadderRow.AddStatement(id + ":" + cyName + " シングルバルブ"));

            var label = cylinder.Mnemonic.DeviceLabel; // ラベルの取得  
            var startNum = cylinder.Mnemonic.StartNum; // ラベルの取得  

            // CYが一致するOperationの取得  
            var cylinderOperations = operations.Where(o => o.Operation.CYId == cylinder.Cylinder.Id).ToList();
            var goOperation = cylinderOperations.Where(o => o.Operation.GoBack == "G").ToList();        // 行きのOperationを取得  
            var backOperation = cylinderOperations.Where(o => o.Operation.GoBack == "B").ToList();      // 帰りのOperationを取得  
            var activeOperation = cylinderOperations.Where(o => o.Operation.GoBack == "A").ToList();    // 作動のOperationを取得  

            // 行き方向自動指令  
            result.AddRange(Common.GoOperation(goOperation, activeOperation, cylinder));

            // 帰り方向自動指令  
            result.AddRange(Common.BackOperation(backOperation, cylinder));

            // 行き方向手動指令  
            result.AddRange(Common.GoManualOperation(goOperation, activeOperation, cylinder));

            // 帰り方向手動指令  
            result.AddRange(Common.BackManualOperation(backOperation, cylinder));

            // Cycleスタート時の方向自動指令  
            bool isFirst = true; // 最初のOperationかどうかのフラグ
            if (cylinder.Cylinder.ProcessStartCycle != null)
            {
                // 修正箇所: List<int> startCycleIds の初期化部分  
                if (cylinder.Cylinder.ProcessStartCycle != null)
                {
                    // ProcessStartCycle をセミコロンで分割し、各要素を整数に変換してリストに格納  
                    List<int> startCycles = cylinder.Cylinder.ProcessStartCycle
                        .Split(';')
                        .Select(int.Parse)
                        .ToList();


                    foreach (var startCycleId in startCycles)
                    {
                        // 各サイクルIDに対して処理を行う  
                        var eachCycle = cycles.FirstOrDefault(c => c.Id == startCycleId);
                        if (eachCycle != null)
                        {
                            // Cycleに関連する処理をここに追加
                            // 例: CycleのラベルをLD命令として追加
                            result.Add(LadderRow.AddLDP(eachCycle.StartDevice));
                            result.Add(LadderRow.AddAND(SettingsManager.Settings.AlwaysON));
                            if (isFirst)
                            {
                                isFirst = false; // 最初のOperationの場合、フラグを更新
                                continue;
                            }
                            result.Add(LadderRow.AddORB()); // 出力命令を追加
                        }
                    }

                    result.Add(LadderRow.AddPLS(label + (startNum + 4)));
                }
            }

            // 行き方向自動保持
            result.Add(LadderRow.AddLDP(label + (startNum + 0).ToString()));
            result.Add(LadderRow.AddORP(label + (startNum + 2).ToString()));
            result.Add(LadderRow.AddSET(label + (startNum + 5).ToString()));

            // 帰り方向自動保持
            result.Add(LadderRow.AddLDP(label + (startNum + 1).ToString()));
            result.Add(LadderRow.AddORP(label + (startNum + 3).ToString()));
            result.Add(LadderRow.AddSET(label + (startNum + 6).ToString()));

            // 行き方向自動保持
            result.Add(LadderRow.AddLDP(label + (startNum + 6).ToString()));
            result.Add(LadderRow.AddORP(SettingsManager.Settings.SoftResetSignal));
            result.Add(LadderRow.AddRST(label + (startNum + 5).ToString()));

            // 帰り方向自動保持
            result.Add(LadderRow.AddLDP(label + (startNum + 5).ToString()));
            result.Add(LadderRow.AddORP(SettingsManager.Settings.SoftResetSignal));
            result.Add(LadderRow.AddRST(label + (startNum + 6).ToString()));

            // 保持出力行き
            result.Add(LadderRow.AddLDI(label + (startNum + 0).ToString()));
            result.Add(LadderRow.AddANI(label + (startNum + 2).ToString()));


            // センサーの取得
            var goSensor = IOAddress.FindByIOText(sensors, "G", plcId, out localErrors);
            var backSensor = IOAddress.FindByIOText(sensors, "G", plcId, out localErrors);

            // 保持出力帰り
            result.Add(LadderRow.AddLDI(label + (startNum + 0).ToString()));
            result.Add(LadderRow.AddANI(label + (startNum + 2).ToString()));
            if (goSensor != null)
            {
                result.Add(LadderRow.AddAND(goSensor));
            }
            else
            {
            }
            result.Add(LadderRow.AddAND(label + (startNum + 5).ToString()));
            result.Add(LadderRow.AddOUT(label + (startNum + 19).ToString()));

            // 保持出力行き
            result.Add(LadderRow.AddLDI(label + (startNum + 1).ToString()));
            result.Add(LadderRow.AddANI(label + (startNum + 3).ToString()));
            if (backSensor != null)
            {
                result.Add(LadderRow.AddAND(backSensor));
            }
            else
            {
            }
            result.Add(LadderRow.AddAND(label + (startNum + 6).ToString()));
            result.Add(LadderRow.AddOUT(label + (startNum + 20).ToString()));

            // 出力検索
            string? valveSearchString = mainViewModel.ValveSearchText;
            var valves = IOAddress.FindByIORange(sensors, valveSearchString, out localErrors);
            var goValve = valves?.FirstOrDefault(v => v.IOName != null && v.IOName.EndsWith(cylinder.Cylinder.Go ?? ""));
            var backValve = valves?.FirstOrDefault(v => v.IOName != null && v.IOName.EndsWith(cylinder.Cylinder.Back ?? ""));

            // 帰り方向
            result.Add(LadderRow.AddLD(label + (startNum + 20).ToString()));
            result.Add(LadderRow.AddOR(label + (startNum + 1).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(label + (startNum + 16).ToString()));

            result.Add(LadderRow.AddLD(label + (startNum + 3).ToString()));
            result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(label + (startNum + 18).ToString()));
            result.Add(LadderRow.AddORB()); // 出力命令を追加
            result.Add(LadderRow.AddOUT(label + (startNum + 9).ToString()));

            // 行き方向のバルブ出力
            if (goValve != null)
            {
                result.Add(LadderRow.AddLD(label + (startNum + 19).ToString()));
                result.Add(LadderRow.AddOR(label + (startNum + 0).ToString()));
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddAND(label + (startNum + 15).ToString()));

                result.Add(LadderRow.AddLD(label + (startNum + 2).ToString()));
                result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddAND(label + (startNum + 17).ToString()));
                result.Add(LadderRow.AddORB()); // 出力命令を追加
                result.Add(LadderRow.AddANI(label + (startNum + 9).ToString()));
                result.Add(LadderRow.AddOUT(goValve.Address));
            }
            else
            {
                localErrors.Add(new OutputError
                {
                    DetailName = cylinder.Cylinder.CYNum ?? "",
                    Message = $"行き方向のバルブ '{cylinder.Cylinder.Go}' が見つかりませんでした。",
                    MnemonicId = 4,
                    ProcessId = cylinder.Cylinder.Id
                });


            }

            errors.AddRange(localErrors); // エラーを追加
            return result;

        }
    }
}