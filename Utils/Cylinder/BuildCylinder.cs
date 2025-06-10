using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services;
using KdxDesigner.Services.Error;
using KdxDesigner.Utils.MnemonicCommon;
using KdxDesigner.ViewModels;

using System.Diagnostics;
using System.Reflection.Emit;

namespace KdxDesigner.Utils.Cylinder
{
    internal class BuildCylinder
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;
        public BuildCylinder(MainViewModel mainViewModel, IErrorAggregator errorAggregator, IIOAddressService ioAddressService)
        {
            _mainViewModel = mainViewModel;
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
        }

        public List<LadderCsvRow> Valve1(
                MnemonicDeviceWithCylinder cylinder,
                List<MnemonicDeviceWithProcessDetail> details,
                List<MnemonicDeviceWithOperation> operations,
                List<MnemonicDeviceWithCylinder> cylinders,
                List<MnemonicTimerDeviceWithOperation> timers,
                List<Error> mnemonicError,
                List<ProsTime> prosTimes,
                List<IO> ioList)
        {
            // ここに単一工程の処理を実装  
            var result = new List<LadderCsvRow>();
            var functions = new CylinderFunction(_mainViewModel, _errorAggregator, cylinder, _ioAddressService);

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
            result.AddRange(functions.GoOperation(goOperation, activeOperation));

            // 帰り方向自動指令  
            result.AddRange(functions.BackOperation(backOperation));

            // 行き方向手動指令  
            result.AddRange(functions.GoManualOperation(goOperation, activeOperation));

            // 帰り方向手動指令  
            result.AddRange(functions.BackManualOperation(backOperation));
                
            // Cycleスタート時の方向自動指令
            result.AddRange(functions.CyclePulse());

           
            // 保持出力
            result.AddRange(functions.Retention(sensors));


            // 出力検索
            string valveSearchString = _mainViewModel.ValveSearchText;
            var valveResult = _ioAddressService.FindByIOText(sensors, valveSearchString, _mainViewModel.SelectedPlc!.Id);
            string? goValve = null;
            switch (valveResult.State)
            {
                case FindIOResultState.FoundOne:
                    goValve = valveResult.SingleAddress;
                    break;

                case FindIOResultState.FoundMultiple:
                    _errorAggregator.AddError(new OutputError { Message = "複数の出力バルブ候補が見つかりました。手動での選択が必要です。", DetailName = "G" });
                    break;

                case FindIOResultState.NotFound:
                    break;
            }

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
                result.Add(LadderRow.AddOUT(goValve));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    DetailName = cylinder.Cylinder.CYNum ?? "",
                    Message = $"行き方向のバルブ '{cylinder.Cylinder.Go}' が見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    ProcessId = cylinder.Cylinder.Id
                });


            }
            return result;

        }

    }
}