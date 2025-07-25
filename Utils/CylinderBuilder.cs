﻿using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services.Error;
using KdxDesigner.Services.IOAddress;
using KdxDesigner.Utils.Cylinder;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Utils
{
    public class CylinderBuilder
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioService;


        public CylinderBuilder(MainViewModel mainViewModel, IErrorAggregator errorAggregator, IIOAddressService ioService)
        {
            _mainViewModel = mainViewModel;
            _errorAggregator = errorAggregator;
            _ioService = ioService;
        }

        public List<LadderCsvRow> GenerateLadder(
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<MnemonicTimerDeviceWithOperation> timers,
            List<MnemonicTimerDeviceWithCylinder> cylinderTymers,
            List<MnemonicSpeedDevice> speed,
            List<Error> mnemonicErrors,
            List<ProsTime> prosTimes,
            List<IO> ioList)
        {
            LadderCsvRow.ResetKeyCounter();
            var result = new List<LadderCsvRow>();
            var builder = new BuildCylinderValve(_mainViewModel, _errorAggregator, _ioService);
            var speedBuilder = new BuildCylinderSpeed(_mainViewModel, _errorAggregator, _ioService);
            var positionBuilder = new BuildCylinderPosition(_mainViewModel, _errorAggregator, _ioService);

            foreach (var cylinder in cylinders)
            {
                switch (cylinder.Cylinder.DriveSub)
                {
                    case 1:
                    case 4:
                    case 10:
                        result.AddRange(builder.Valve1(
                            cylinder,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            ioList));
                        break;
                    case 15:
                        result.AddRange(builder.Motor(
                            cylinder,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            ioList));
                        break;
                    case 2:
                    case 3:
                    case 5:
                    case 6:
                        result.AddRange(builder.Valve2(
                            cylinder,
                            details,
                            operations,
                            cylinders,
                            timers,
                            mnemonicErrors,
                            prosTimes,
                            ioList));
                        break;
                    case 7:
                    case 8:
                    case 9:
                        result.AddRange(speedBuilder.Flow1(
                            cylinder,
                            details,
                            operations,
                            cylinders,
                            timers,
                            speed,
                            mnemonicErrors,
                            prosTimes,
                            ioList));
                        break;
                    case 14:
                        result.AddRange(positionBuilder.Servo(
                            cylinder,
                            details,
                            operations,
                            cylinders,
                            timers,
                            cylinderTymers,
                            speed,
                            mnemonicErrors,
                            prosTimes,
                            ioList));
                        break;
                    case 16:
                        result.AddRange(speedBuilder.Inverter(
                            cylinder,
                            details,
                            operations,
                            cylinders,
                            timers,
                            cylinderTymers,
                            speed,
                            mnemonicErrors,
                            prosTimes,
                            ioList));
                        break;
                    default:
                        break;
                }
            }
            return result;
        }

    }
}
