using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services;
using KdxDesigner.Services.Error;
using KdxDesigner.Utils.MnemonicCommon;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Utils.Cylinder
{
    internal class CylinderFunction
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;
        private readonly IIOAddressService _ioAddressService;
        private readonly MnemonicDeviceWithCylinder _cylinder;
        private string _label; // ラベルの取得
        private int _startNum; // ラベルの取得
        private string? _speedDevice;

        // コンストラクタでMainViewModelをインジェクト
        public CylinderFunction(
            MainViewModel mainViewModel,
            IErrorAggregator errorAggregator,
            MnemonicDeviceWithCylinder cylinder,
            IIOAddressService ioAddressService,
            string? speedDevice)
        {
            _mainViewModel = mainViewModel;
            _errorAggregator = errorAggregator;
            _cylinder = cylinder;
            _ioAddressService = ioAddressService; // IIOAddressServiceのインジェクト
            _startNum = cylinder.Mnemonic.StartNum ?? 0; // ラベルの取得
            _label = cylinder.Mnemonic.DeviceLabel ?? "M"; // ラベルの取得
            _speedDevice = speedDevice;
        }

        public List<LadderCsvRow> GoOperation(
            List<MnemonicDeviceWithOperation> goOperation,
            List<MnemonicDeviceWithOperation> activeOperation)
        {
            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト
            bool isFirst = true; // 最初のOperationかどうかのフラグ

            // 行き方向自動指令
            foreach (var go in goOperation)
            {
                var operationLabel = go.Mnemonic.DeviceLabel; // 行きのラベル
                var operationOutcoil = go.Mnemonic.StartNum; // 出力番号の取得
                result.Add(LadderRow.AddLD(operationLabel + (operationOutcoil + 6).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddANI(operationLabel + (operationOutcoil + 17).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddAND(operationLabel + (operationOutcoil + 0).ToString())); // ラベルのLD命令を追加
                if (isFirst)
                {
                    isFirst = false; // 最初のOperationの場合、フラグを更新
                    continue;
                }
                result.Add(LadderRow.AddORB()); // 出力命令を追加
            }

            foreach (var go in activeOperation)
            {
                var operationLabel = go.Mnemonic.DeviceLabel; // 行きのラベル
                var operationOutcoil = go.Mnemonic.StartNum; // 出力番号の取得
                result.Add(LadderRow.AddLD(operationLabel + (operationOutcoil + 6).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddANI(operationLabel + (operationOutcoil + 17).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddAND(operationLabel + (operationOutcoil + 0).ToString())); // ラベルのLD命令を追加
                if (isFirst)
                {
                    isFirst = false; // 最初のOperationの場合、フラグを更新
                    continue;
                }
                result.Add(LadderRow.AddORB()); // 出力命令を追加
            }

            result.Add(LadderRow.AddOUT(_label + (_startNum + 0).ToString())); // ラベルのLD命令を追加

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> BackOperation(
            List<MnemonicDeviceWithOperation> backOperation)
        {
            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト
            bool isFirst = true; // 最初のOperationかどうかのフラグ

            // 行き方向自動指令
            foreach (var back in backOperation)
            {
                var operationLabel = back.Mnemonic.DeviceLabel; // 行きのラベル
                var operationOutcoil = back.Mnemonic.StartNum; // 出力番号の取得
                result.Add(LadderRow.AddLD(operationLabel + (operationOutcoil + 6).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddANI(operationLabel + (operationOutcoil + 17).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddAND(operationLabel + (operationOutcoil + 0).ToString())); // ラベルのLD命令を追加
                if (isFirst)
                {
                    isFirst = false; // 最初のOperationの場合、フラグを更新
                    continue;
                }
                result.Add(LadderRow.AddORB()); // 出力命令を追加
            }
            result.Add(LadderRow.AddOUT(_label + (_startNum + 0).ToString())); // ラベルのLD命令を追加
            result.Add(LadderRow.AddOUT(_label + (_startNum + 0).ToString())); // ラベルのLD命令を追加

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> GoManualOperation(
            List<MnemonicDeviceWithOperation> goOperation,
            List<MnemonicDeviceWithOperation> activeOperation)
        {
            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト
            bool isFirst = true; // 最初のOperationかどうかのフラグ

            // 行き方向自動指令
            foreach (var go in goOperation)
            {
                var operationLabel = go.Mnemonic.DeviceLabel; // 行きのラベル
                var operationOutcoil = go.Mnemonic.StartNum; // 出力番号の取得
                result.Add(LadderRow.AddLD(operationLabel + (operationOutcoil + 6).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddANI(operationLabel + (operationOutcoil + 17).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddAND(operationLabel + (operationOutcoil + 2).ToString())); // ラベルのLD命令を追加
                if (isFirst)
                {
                    isFirst = false; // 最初のOperationの場合、フラグを更新
                    continue;
                }
                result.Add(LadderRow.AddORB()); // 出力命令を追加
            }

            foreach (var go in activeOperation)
            {
                var operationLabel = go.Mnemonic.DeviceLabel; // 行きのラベル
                var operationOutcoil = go.Mnemonic.StartNum; // 出力番号の取得
                result.Add(LadderRow.AddLD(operationLabel + (operationOutcoil + 6).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddANI(operationLabel + (operationOutcoil + 17).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddAND(operationLabel + (operationOutcoil + 2).ToString())); // ラベルのLD命令を追加
                if (isFirst)
                {
                    isFirst = false; // 最初のOperationの場合、フラグを更新
                    continue;
                }
                result.Add(LadderRow.AddORB()); // 出力命令を追加
            }

            result.Add(LadderRow.AddOUT(_label + (_startNum + 0).ToString())); // ラベルのLD命令を追加

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> BackManualOperation(
            List<MnemonicDeviceWithOperation> backOperation)
        {
            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト
            bool isFirst = true; // 最初のOperationかどうかのフラグ

            // 行き方向自動指令
            foreach (var back in backOperation)
            {
                var operationLabel = back.Mnemonic.DeviceLabel; // 行きのラベル
                var operationOutcoil = back.Mnemonic.StartNum; // 出力番号の取得
                result.Add(LadderRow.AddLD(operationLabel + (operationOutcoil + 6).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddANI(operationLabel + (operationOutcoil + 17).ToString())); // ラベルのLD命令を追加
                result.Add(LadderRow.AddAND(operationLabel + (operationOutcoil + 2).ToString())); // ラベルのLD命令を追加
                if (isFirst)
                {
                    isFirst = false; // 最初のOperationの場合、フラグを更新
                    continue;
                }
                result.Add(LadderRow.AddORB()); // 出力命令を追加
            }
            result.Add(LadderRow.AddOUT(_label + (_startNum + 0).ToString())); // ラベルのLD命令を追加
            result.Add(LadderRow.AddOUT(_label + (_startNum + 0).ToString())); // ラベルのLD命令を追加

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> OutputRetention()
        {
            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト

            // 行き方向自動保持
            result.Add(LadderRow.AddLDP(_label + (_startNum + 0).ToString()));
            result.Add(LadderRow.AddORP(_label + (_startNum + 2).ToString()));
            result.Add(LadderRow.AddSET(_label + (_startNum + 5).ToString()));

            // 帰り方向自動保持
            result.Add(LadderRow.AddLDP(_label + (_startNum + 1).ToString()));
            result.Add(LadderRow.AddORP(_label + (_startNum + 3).ToString()));
            result.Add(LadderRow.AddSET(_label + (_startNum + 6).ToString()));

            // 行き方向自動保持
            result.Add(LadderRow.AddLDP(_label + (_startNum + 6).ToString()));
            result.Add(LadderRow.AddORP(SettingsManager.Settings.SoftResetSignal));
            result.Add(LadderRow.AddRST(_label + (_startNum + 5).ToString()));

            // 帰り方向自動保持
            result.Add(LadderRow.AddLDP(_label + (_startNum + 5).ToString()));
            result.Add(LadderRow.AddORP(SettingsManager.Settings.SoftResetSignal));
            result.Add(LadderRow.AddRST(_label + (_startNum + 6).ToString()));

            // 保持出力行き
            result.Add(LadderRow.AddLDI(_label + (_startNum + 0).ToString()));
            result.Add(LadderRow.AddANI(_label + (_startNum + 2).ToString()));

            return result; // 生成されたLadderCsvRowのリストを返す
        }


        public List<LadderCsvRow> CyclePulse()
        {
            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト
            bool isFirst = true; // 最初のOperationかどうかのフラグ


            if (_cylinder.Cylinder.ProcessStartCycle != null)
            {
                // 修正箇所: List<int> startCycleIds の初期化部分  
                if (_cylinder.Cylinder.ProcessStartCycle != null)
                {
                    // ProcessStartCycle をセミコロンで分割し、各要素を整数に変換してリストに格納  
                    List<int> startCycles = _cylinder.Cylinder.ProcessStartCycle
                        .Split(';')
                        .Select(int.Parse)
                        .ToList();


                    foreach (var startCycleId in startCycles)
                    {
                        // 各サイクルIDに対して処理を行う  
                        var eachCycle = _mainViewModel.Cycles.FirstOrDefault(c => c.Id == startCycleId);
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
                    result.Add(LadderRow.AddPLS(_label + (_startNum + 4)));
                }
            }

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> Retention(List<IO> sensors)
        {
            // センサーの取得
            var findGoSensorResult = _ioAddressService.FindByIOText(sensors, "G", _mainViewModel.SelectedPlc!.Id);
            string? goSensor = null;
            switch (findGoSensorResult.State)
            {
                case FindIOResultState.FoundOne:
                    goSensor = findGoSensorResult.SingleAddress;
                    break;

                case FindIOResultState.FoundMultiple:
                    // ★ サービス内ではUIを呼ばない。代わりにエラーとして報告する。
                    // ViewModel はこのエラーを受けて、ユーザーに選択を促すUIを表示する。
                    _errorAggregator.AddError(new OutputError { Message = "センサー 'G' で複数の候補が見つかりました。手動での選択が必要です。", DetailName = "G" });
                    break;

                case FindIOResultState.NotFound:
                    // エラーはサービス内で既に追加されているので、ここでは何もしない。
                    break;
            }

            var findBackSensorResult = _ioAddressService.FindByIOText(sensors, "B", _mainViewModel.SelectedPlc!.Id);
            string? backSensor = null;
            switch (findBackSensorResult.State)
            {
                case FindIOResultState.FoundOne:
                    backSensor = findBackSensorResult.SingleAddress;
                    break;

                case FindIOResultState.FoundMultiple:
                    // ★ サービス内ではUIを呼ばない。代わりにエラーとして報告する。
                    // ViewModel はこのエラーを受けて、ユーザーに選択を促すUIを表示する。
                    _errorAggregator.AddError(new OutputError { Message = "センサー 'B' で複数の候補が見つかりました。手動での選択が必要です。", DetailName = "G" });
                    break;

                case FindIOResultState.NotFound:
                    // エラーはサービス内で既に追加されているので、ここでは何もしない。
                    break;
            }


            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト
            result.Add(LadderRow.AddLDI(_label + (_startNum + 0).ToString()));
            result.Add(LadderRow.AddANI(_label + (_startNum + 2).ToString()));
            if (goSensor != null)
            {
                result.Add(LadderRow.AddAND(goSensor));
            }
            else
            {
            }
            result.Add(LadderRow.AddAND(_label + (_startNum + 5).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_startNum + 19).ToString()));

            // 保持出力行き
            result.Add(LadderRow.AddLDI(_label + (_startNum + 1).ToString()));
            result.Add(LadderRow.AddANI(_label + (_startNum + 3).ToString()));
            if (backSensor != null)
            {
                result.Add(LadderRow.AddAND(backSensor));
            }
            else
            {
            }
            result.Add(LadderRow.AddAND(_label + (_startNum + 6).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_startNum + 20).ToString()));



            return result; // 生成されたLadderCsvRowのリストを返す
        }
        public List<LadderCsvRow> RetentionFlow(List<IO> sensors)
        {
            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト

            // センサーの取得
            var findGoSensorResult = _ioAddressService.FindByIOText(sensors, "G", _mainViewModel.SelectedPlc!.Id);
            string? goSensor = null;
            switch (findGoSensorResult.State)
            {
                case FindIOResultState.FoundOne:
                    goSensor = findGoSensorResult.SingleAddress;
                    break;

                case FindIOResultState.FoundMultiple:
                    // ★ サービス内ではUIを呼ばない。代わりにエラーとして報告する。
                    // ViewModel はこのエラーを受けて、ユーザーに選択を促すUIを表示する。
                    _errorAggregator.AddError(new OutputError { Message = "センサー 'G' で複数の候補が見つかりました。手動での選択が必要です。", DetailName = "G" });
                    break;

                case FindIOResultState.NotFound:
                    // エラーはサービス内で既に追加されているので、ここでは何もしない。
                    break;
            }

            var findBackSensorResult = _ioAddressService.FindByIOText(sensors, "B", _mainViewModel.SelectedPlc!.Id);
            string? backSensor = null;
            switch (findBackSensorResult.State)
            {
                case FindIOResultState.FoundOne:
                    backSensor = findBackSensorResult.SingleAddress;
                    break;

                case FindIOResultState.FoundMultiple:
                    // ★ サービス内ではUIを呼ばない。代わりにエラーとして報告する。
                    // ViewModel はこのエラーを受けて、ユーザーに選択を促すUIを表示する。
                    _errorAggregator.AddError(new OutputError { Message = "センサー 'B' で複数の候補が見つかりました。手動での選択が必要です。", DetailName = "G" });
                    break;

                case FindIOResultState.NotFound:
                    // エラーはサービス内で既に追加されているので、ここでは何もしない。
                    break;
            }

            result.Add(LadderRow.AddLDI(_label + (_startNum + 0).ToString()));
            result.Add(LadderRow.AddANI(_label + (_startNum + 2).ToString()));
            if (goSensor != null)
            {
                result.Add(LadderRow.AddAND(goSensor));
            }
            else
            {
            }
            result.Add(LadderRow.AddAND(_label + (_startNum + 5).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_startNum + 19).ToString()));

            if (_speedDevice != null)
            {
                result.AddRange(LadderRow.AddMOVSet("K1", _speedDevice)); // スピードデバイスの設定
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    DetailName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $"速度設定用のデバイスが見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    ProcessId = _cylinder.Cylinder.Id
                });
            }


            // 保持出力行き
            result.Add(LadderRow.AddLDI(_label + (_startNum + 1).ToString()));
            result.Add(LadderRow.AddANI(_label + (_startNum + 3).ToString()));
            if (backSensor != null)
            {
                result.Add(LadderRow.AddAND(backSensor));
            }
            else
            {
            }
            result.Add(LadderRow.AddAND(_label + (_startNum + 6).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_startNum + 20).ToString()));

            if (_speedDevice != null)
            {
                result.AddRange(LadderRow.AddMOVSet("K5", _speedDevice)); // スピードデバイスの設定
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    DetailName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $"速度設定用のデバイスが見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    ProcessId = _cylinder.Cylinder.Id
                });
            }
            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> FlowOperate()
        {
            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト

            if (_speedDevice == null)
            {
                _errorAggregator.AddError(new OutputError
                {
                    DetailName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $"速度設定用のデバイスが見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    ProcessId = _cylinder.Cylinder.Id
                });
                return result; // スピードデバイスがない場合は空のリストを返す
            }

            for (int i = 0; i < 10; i++)
            {
                result.AddRange(LadderRow.AddLDE(_speedDevice, ("K" + i.ToString())));
                result.Add(LadderRow.AddAND(_label + (_startNum + (10)).ToString()));
                result.Add(LadderRow.AddOUT(_label + (_startNum + (i + 20)).ToString()));
            }

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> FlowOK()
        {
            var result = new List<LadderCsvRow>();

            // 行きOK
            result.Add(LadderRow.AddLD(_label + (_startNum + 19).ToString()));
            result.Add(LadderRow.AddOR(_label + (_startNum + 0).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(_label + (_startNum + 15).ToString()));

            result.Add(LadderRow.AddLD(_label + (_startNum + 2).ToString()));
            result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(_label + (_startNum + 17).ToString()));
            result.Add(LadderRow.AddORB()); // 出力命令を追加
            result.Add(LadderRow.AddANI(_label + (_startNum + 9).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_startNum + 35)));

            // 帰りOK
            result.Add(LadderRow.AddLD(_label + (_startNum + 20).ToString()));
            result.Add(LadderRow.AddOR(_label + (_startNum + 1).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(_label + (_startNum + 16).ToString()));

            result.Add(LadderRow.AddLD(_label + (_startNum + 3).ToString()));
            result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(_label + (_startNum + 18).ToString()));
            result.Add(LadderRow.AddORB()); // 出力命令を追加
            result.Add(LadderRow.AddOUT(_label + (_startNum + 36)));

            // 指令OK
            result.Add(LadderRow.AddLD(_label + (_startNum + 35)));
            result.Add(LadderRow.AddOR(_label + (_startNum + 36)));
            result.Add(LadderRow.AddOUT(_label + (_startNum + 37)));

            return result; // 生成されたLadderCsvRowのリストを返す
        }


        public List<LadderCsvRow> SingleValve(List<IO> sensors)
        {
            var result = new List<LadderCsvRow>();

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
            result.Add(LadderRow.AddLD(_label + (_startNum + 20).ToString()));
            result.Add(LadderRow.AddOR(_label + (_startNum + 1).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(_label + (_startNum + 16).ToString()));

            result.Add(LadderRow.AddLD(_label + (_startNum + 3).ToString()));
            result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(_label + (_startNum + 18).ToString()));
            result.Add(LadderRow.AddORB()); // 出力命令を追加
            result.Add(LadderRow.AddOUT(_label + (_startNum + 9).ToString()));

            // 行き方向のバルブ出力
            if (goValve != null)
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 19).ToString()));
                result.Add(LadderRow.AddOR(_label + (_startNum + 0).ToString()));
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddAND(_label + (_startNum + 15).ToString()));

                result.Add(LadderRow.AddLD(_label + (_startNum + 2).ToString()));
                result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddAND(_label + (_startNum + 17).ToString()));
                result.Add(LadderRow.AddORB()); // 出力命令を追加
                result.Add(LadderRow.AddANI(_label + (_startNum + 9).ToString()));
                result.Add(LadderRow.AddOUT(goValve));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    DetailName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $"行き方向のバルブ '{_cylinder.Cylinder.Go}' が見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    ProcessId = _cylinder.Cylinder.Id
                });


            }

            return result; // 生成されたLadderCsvRowのリストを返す

        }

        public List<LadderCsvRow> DoubleValve(List<IO> sensors)
        {
            var result = new List<LadderCsvRow>();

            string valveSearchString = _mainViewModel.ValveSearchText;
            var valveResult = _ioAddressService.FindByIOText(sensors, valveSearchString, _mainViewModel.SelectedPlc!.Id);
            string? goValve = null;
            string? backValve = null;

            switch (valveResult.State)
            {
                case FindIOResultState.FoundOne:
                    _errorAggregator.AddError(new OutputError
                    {
                        MnemonicId = (int)MnemonicType.CY,
                        ProcessId = _cylinder.Cylinder.Id,
                        Message = "出力バルブ候補が1つしかありません。",
                        DetailName = _cylinder.Cylinder.CYNum ?? ""
                    });

                    break;

                case FindIOResultState.FoundMultiple:

                    // 前進（Go）バルブの出力先が設定されているかチェック
                    if (string.IsNullOrEmpty(_cylinder.Cylinder.Go))
                    {
                        _errorAggregator.AddError(new OutputError
                        {
                            MnemonicId = (int)MnemonicType.CY,
                            ProcessId = _cylinder.Cylinder.Id,
                            Message = $"シリンダ「{_cylinder.Cylinder.CYNum}」の前進（Go）バルブ出力先が設定されていません。",
                            DetailName = _cylinder.Cylinder.CYNum ?? ""
                        });
                    }
                    else
                    {
                        // 設定されている場合のみ、一致するアドレスを探す
                        goValve = valveResult.MultipleMatches?
                            .Where(m => m.IOName != null && m.IOName.EndsWith(_cylinder.Cylinder.Go))
                            .Select(m => m.Address)
                            .FirstOrDefault();
                    }

                    // 後退（Back）バルブの出力先が設定されているかチェック
                    if (string.IsNullOrEmpty(_cylinder.Cylinder.Back))
                    {
                        _errorAggregator.AddError(new OutputError
                        {
                            MnemonicId = (int)MnemonicType.CY,
                            ProcessId = _cylinder.Cylinder.Id,
                            Message = $"シリンダ「{_cylinder.Cylinder.CYNum}」の後退（Back）バルブ出力先が設定されていません。",
                            DetailName = _cylinder.Cylinder.CYNum ?? ""
                        });
                    }
                    else
                    {
                        // 設定されている場合のみ、一致するアドレスを探す
                        backValve = valveResult.MultipleMatches?
                            .Where(m => m.IOName != null && m.IOName.EndsWith(_cylinder.Cylinder.Back))
                            .Select(m => m.Address)
                            .FirstOrDefault();
                    }
                    break;

                case FindIOResultState.NotFound:
                    break;
            }


            // 行き方向のバルブ出力
            if (goValve != null)
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 19).ToString()));
                result.Add(LadderRow.AddOR(_label + (_startNum + 0).ToString()));
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddAND(_label + (_startNum + 15).ToString()));

                result.Add(LadderRow.AddLD(_label + (_startNum + 2).ToString()));
                result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddAND(_label + (_startNum + 17).ToString()));
                result.Add(LadderRow.AddORB()); // 出力命令を追加
                result.Add(LadderRow.AddANI(_label + (_startNum + 9).ToString()));
                result.Add(LadderRow.AddOUT(goValve));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    DetailName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $"行き方向のバルブ '{_cylinder.Cylinder.Go}' が見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    ProcessId = _cylinder.Cylinder.Id
                });


            }

            if (backValve != null)
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 20).ToString()));
                result.Add(LadderRow.AddOR(_label + (_startNum + 1).ToString()));
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddAND(_label + (_startNum + 16).ToString()));
                result.Add(LadderRow.AddLD(_label + (_startNum + 3).ToString()));
                result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddAND(_label + (_startNum + 18).ToString()));
                result.Add(LadderRow.AddORB()); // 出力命令を追加
                result.Add(LadderRow.AddOUT(backValve));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    DetailName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $"帰り方向のバルブ '{_cylinder.Cylinder.Back}' が見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    ProcessId = _cylinder.Cylinder.Id
                });
            }
            return result; // 生成されたLadderCsvRowのリストを返す
        }


        public List<LadderCsvRow> FlowValve(List<IO> sensors)
        {
            var result = new List<LadderCsvRow>();

            string valveSearchString = "IN";
            var valveResult = _ioAddressService.FindByIOText(sensors, valveSearchString, _mainViewModel.SelectedPlc!.Id);
            string? valve1 = null;
            string? valve2 = null;
            string? valve3 = null;
            string? valve4 = null;
            string? valve5 = null;
            string? valve6 = null;

            switch (valveResult.State)
            {
                case FindIOResultState.FoundOne:
                    _errorAggregator.AddError(new OutputError { Message = "複数の出力バルブ候補が見つかりました。手動での選択が必要です。", DetailName = "G" });
                    return result;

                case FindIOResultState.FoundMultiple:
                    valveResult.MultipleMatches?.ForEach(m =>
                    {
                        if (m.IOName != null)
                        {
                            if (m.IOName.EndsWith("1")) valve1 = m.Address;
                            else if (m.IOName.EndsWith("2")) valve2 = m.Address;
                            else if (m.IOName.EndsWith("3")) valve3 = m.Address;
                            else if (m.IOName.EndsWith("4")) valve4 = m.Address;
                            else if (m.IOName.EndsWith("5")) valve5 = m.Address;
                            else if (m.IOName.EndsWith("6")) valve6 = m.Address;
                        }
                    });
                    break;
                case FindIOResultState.NotFound:
                    _errorAggregator.AddError(new OutputError { Message = "複数の出力バルブ候補が見つかりました。手動での選択が必要です。", DetailName = "G" });
                    return result;
            }

            // 帰り方向
            result.Add(LadderRow.AddLD(_label + (_startNum + 20).ToString()));
            result.Add(LadderRow.AddOR(_label + (_startNum + 1).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(_label + (_startNum + 16).ToString()));

            result.Add(LadderRow.AddLD(_label + (_startNum + 3).ToString()));
            result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(_label + (_startNum + 18).ToString()));
            result.Add(LadderRow.AddORB()); // 出力命令を追加
            result.Add(LadderRow.AddOUT(_label + (_startNum + 9).ToString()));

            // IN1の出力
            if (valve1 != null && _speedDevice != null)
            {
                result.AddRange(LadderRow.AddLDG(_speedDevice, "K5"));
                result.AddRange(LadderRow.AddANDN(_speedDevice, "K0"));
                result.Add(LadderRow.AddOUT(valve1));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    DetailName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $" '{_cylinder.Cylinder.CYNum}'流量バルブIN1 が見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    ProcessId = _cylinder.Cylinder.Id
                });
            }

            // IN2の出力
            if (valve2 != null)
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 21).ToString()));
                result.Add(LadderRow.AddOR(_label + (_startNum + 26).ToString()));
                result.Add(LadderRow.AddOUT(valve2));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    DetailName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $" '{_cylinder.Cylinder.CYNum}'流量バルブIN2 が見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    ProcessId = _cylinder.Cylinder.Id
                });
            }

            // IN3の出力
            if (valve3 != null)
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 22).ToString()));
                result.Add(LadderRow.AddOR(_label + (_startNum + 27).ToString()));
                result.Add(LadderRow.AddOUT(valve3));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    DetailName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $" '{_cylinder.Cylinder.CYNum}'流量バルブIN3 が見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    ProcessId = _cylinder.Cylinder.Id
                });
            }

            // IN4の出力
            if (valve4 != null)
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 23).ToString()));
                result.Add(LadderRow.AddOR(_label + (_startNum + 28).ToString()));
                result.Add(LadderRow.AddOUT(valve4));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    DetailName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $" '{_cylinder.Cylinder.CYNum}'流量バルブIN4 が見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    ProcessId = _cylinder.Cylinder.Id
                });
            }

            // IN5の出力
            if (valve5 != null)
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 24).ToString()));
                result.Add(LadderRow.AddOR(_label + (_startNum + 29).ToString()));
                result.Add(LadderRow.AddOUT(valve5));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    DetailName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $" '{_cylinder.Cylinder.CYNum}'流量バルブIN5 が見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    ProcessId = _cylinder.Cylinder.Id
                });
            }

            // IN6の出力
            if (valve6 != null)
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 25).ToString()));
                result.Add(LadderRow.AddOR(_label + (_startNum + 30).ToString()));
                result.Add(LadderRow.AddOUT(valve6));
            }
            else
            {
                _errorAggregator.AddError(new OutputError
                {
                    DetailName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $" '{_cylinder.Cylinder.CYNum}'流量バルブIN6 が見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    ProcessId = _cylinder.Cylinder.Id
                });
            }

            return result; // 生成されたLadderCsvRowのリストを返す

        }


    }
}
