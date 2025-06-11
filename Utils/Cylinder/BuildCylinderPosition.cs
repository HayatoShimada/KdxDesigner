using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services;
using KdxDesigner.Services.Error;
using KdxDesigner.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Utils.Cylinder
{
    class BuildCylinderPosition
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;
        public BuildCylinderPosition(MainViewModel mainViewModel, IErrorAggregator errorAggregator, IIOAddressService ioAddressService)
        {
            _mainViewModel = mainViewModel;
            _errorAggregator = errorAggregator;
            _ioAddressService = ioAddressService;
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


            return result;
        }
    }
}
