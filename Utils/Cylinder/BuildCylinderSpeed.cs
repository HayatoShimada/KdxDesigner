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
            result.AddRange(functions.RetentionFlow(sensors));

            // 速度指令
            result.AddRange(functions.FlowOperate());

            // 出力OK
            result.AddRange(functions.FlowOK());


            return result;

        }


    }
}