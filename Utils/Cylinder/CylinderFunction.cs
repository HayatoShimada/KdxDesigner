using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services;
using KdxDesigner.Services.Error;
using KdxDesigner.Utils.MnemonicCommon;
using KdxDesigner.ViewModels;

using System;
using System.Xml.Linq;

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
            _startNum = cylinder.Mnemonic.StartNum; // ラベルの取得
            _label = cylinder.Mnemonic.DeviceLabel; // ラベルの取得
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

            if (!string.IsNullOrWhiteSpace(_cylinder.Cylinder.ProcessStartCycle))
            {
                // ★ 1. Split と int.Parse を安全に行う
                List<int> startCycles = _cylinder.Cylinder.ProcessStartCycle
                    .Split(';', StringSplitOptions.RemoveEmptyEntries) // 空の要素を自動で削除
                    .Select(idString => {
                        int.TryParse(idString.Trim(), out int id); // 空白を除去し、変換を試みる
                        return id;
                    })
                    .Where(id => id != 0) // 変換に失敗した(0になった)要素を除外
                    .ToList();

                // ★ 2. isFirst のロジックを foreach ループの外側で扱う方がシンプル
                bool isFirstCycleInLoop = true;
                foreach (var startCycleId in startCycles)
                {
                    // 各サイクルIDに対して処理を行う  
                    var eachCycle = _mainViewModel.Cycles.FirstOrDefault(c => c.Id == startCycleId);
                    if (eachCycle != null)
                    {
                        if (!isFirstCycleInLoop)
                        {
                            // 2つ目以降のサイクルの場合はORBを追加
                            result.Add(LadderRow.AddORB());
                        }

                        // Cycleに関連する処理をここに追加
                        result.Add(LadderRow.AddLDP(eachCycle.StartDevice));
                        result.Add(LadderRow.AddAND(SettingsManager.Settings.AlwaysON));

                        isFirstCycleInLoop = false; // フラグを更新
                    }
                }

                // ★ 3. ループで有効なサイクルが1つでも処理された場合にのみ、PLSを出力
                if (!isFirstCycleInLoop)
                {
                    result.Add(LadderRow.AddPLS(_label + (_startNum + 4)));
                }
            }

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> Excitation(List<IO> sensors)
        {
            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト
            result.Add(LadderRow.AddLDI(_label + (_startNum + 0).ToString()));
            result.Add(LadderRow.AddANI(_label + (_startNum + 2).ToString()));
            result.Add(LadderRow.AddAND(_label + (_startNum + 5).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_startNum + 19).ToString()));

            result.Add(LadderRow.AddLDI(_label + (_startNum + 1).ToString()));
            result.Add(LadderRow.AddANI(_label + (_startNum + 3).ToString()));
            result.Add(LadderRow.AddAND(_label + (_startNum + 6).ToString()));
            result.Add(LadderRow.AddOUT(_label + (_startNum + 20).ToString()));

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> Retention(List<IO> sensors)
        {
            // センサーの取得
            var goSensor = _ioAddressService.GetSingleAddress(
                sensors, "G", 
                false, 
                _cylinder.Cylinder.CYNum,
                _cylinder.Cylinder.Id,
                null);

            var backSensor = _ioAddressService.GetSingleAddress(
                sensors, 
                "B", false, 
                _cylinder.Cylinder.CYNum, 
                _cylinder.Cylinder.Id,
                null);

            List<LadderCsvRow> result = new(); // 生成されるLadderCsvRowのリスト
            result.Add(LadderRow.AddLDI(_label + (_startNum + 0).ToString()));
            result.Add(LadderRow.AddANI(_label + (_startNum + 2).ToString()));
            if (goSensor != null)
            {
                result.Add(LadderRow.AddAND(goSensor));
            }
            else
            {
                _errorAggregator.AddError(
                    new OutputError
                    {
                        Message = "センサー 'G' が見つかりませんでした。",
                        RecordName = _cylinder.Cylinder.CYNum,
                        RecordId = _cylinder.Cylinder.Id,
                        MnemonicId = (int)MnemonicType.CY,
                        IsCritical = false
                    });
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
            var goSensor = _ioAddressService.GetSingleAddress(sensors, "G", false, _cylinder.Cylinder.CYNum, _cylinder.Cylinder.Id, null);
            var backSensor = _ioAddressService.GetSingleAddress(sensors, "B", false, _cylinder.Cylinder.CYNum, _cylinder.Cylinder.Id, null);

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
                    RecordName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $"速度設定用のデバイスが見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id,
                    IsCritical = false
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
                    RecordName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $"速度設定用のデバイスが見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id,
                    IsCritical = false
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
                    RecordName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $"速度設定用のデバイスが見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id,
                    IsCritical = false
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

            string? goValve = null;

            if (_cylinder.Cylinder.CYNameSub != null)
            {
                goValve = _ioAddressService.
                    GetSingleAddress(
                    sensors,
                    _cylinder.Cylinder.Go + _cylinder.Cylinder.CYNameSub, 
                    true, 
                    _cylinder.Cylinder.CYNum, 
                    _cylinder.Cylinder.Id, 
                    null);
            }
            else
            {
                goValve = _ioAddressService.
                    GetSingleAddress(
                    sensors, 
                    valveSearchString, 
                    true, 
                    _cylinder.Cylinder.CYNum, 
                    _cylinder.Cylinder.Id,
                    null);

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
                    RecordName = _cylinder.Cylinder.CYNum ?? "",
                    Message = $"行き方向のバルブ '{_cylinder.Cylinder.Go}' が見つかりませんでした。",
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id
                });
            }
            return result; // 生成されたLadderCsvRowのリストを返す

        }

        public List<LadderCsvRow> DoubleValve(List<IO> sensors)
        {
            var result = new List<LadderCsvRow>();

            // 1. GetAddressRange を使って、"SV" を含むすべてのバルブ候補を取得
            string? valveSearchString = _mainViewModel.ValveSearchText;
            var valveCandidates = _ioAddressService.GetAddressRange(sensors, valveSearchString ?? "SV", _cylinder.Cylinder.CYNum, _cylinder.Cylinder.Id, errorIfNotFound: true);

            // 2. ダブルバルブには最低2つの候補が必要なため、候補数をチェック
            if (valveCandidates.Count < 2)
            {
                _errorAggregator.AddError(new OutputError
                {
                    Message = $"ダブルバルブを特定できません。バルブ検索文字列 '{valveSearchString}' に一致するIOが2件未満です。",
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id,
                    RecordName = _cylinder.Cylinder.CYNum ?? ""
                });
                // 候補が足りないが、個別のエラーを報告するために処理は続行
            }

            // 3. ヘルパーメソッドを使い、Go/Backバルブをそれぞれ検索
            var goValveAddress = FindValveAddress(valveCandidates, _cylinder.Cylinder.Go, "前進 (Go)");
            var backValveAddress = FindValveAddress(valveCandidates, _cylinder.Cylinder.Back, "後退 (Back)");

            // 4. 見つかったアドレスに基づいてラダーを生成（エラー処理はヘルパーが担当）

            // 行き方向のバルブ出力
            if (goValveAddress != null)
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 19).ToString()));
                result.Add(LadderRow.AddOR(_label + (_startNum + 0).ToString()));
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddAND(_label + (_startNum + 15).ToString()));

                result.Add(LadderRow.AddLD(_label + (_startNum + 2).ToString()));
                result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddAND(_label + (_startNum + 17).ToString()));
                result.Add(LadderRow.AddORB());

                // backValveAddress が見つかっている場合、それとANDNでインターロックを組む
                if (backValveAddress != null)
                {
                    result.Add(LadderRow.AddANI(backValveAddress));
                }

                result.Add(LadderRow.AddOUT(goValveAddress));
            }
            // elseの場合、エラーはFindValveAddress内で記録済み

            // 帰り方向のバルブ出力
            if (backValveAddress != null)
            {
                result.Add(LadderRow.AddLD(_label + (_startNum + 20).ToString()));
                result.Add(LadderRow.AddOR(_label + (_startNum + 1).ToString()));
                result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddAND(_label + (_startNum + 16).ToString()));

                result.Add(LadderRow.AddLD(_label + (_startNum + 3).ToString()));
                result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
                result.Add(LadderRow.AddAND(_label + (_startNum + 18).ToString()));
                result.Add(LadderRow.AddORB());

                // goValveAddress が見つかっている場合、それとANDNでインターロックを組む
                if (goValveAddress != null)
                {
                    result.Add(LadderRow.AddANI(goValveAddress));
                }

                result.Add(LadderRow.AddOUT(backValveAddress));
            }
            // elseの場合、エラーはFindValveAddress内で記録済み

            return result;
        }


        public List<LadderCsvRow> FlowValve(List<IO> sensors)
        {
            var result = new List<LadderCsvRow>();
            const string valveSearchString = "IN";

            // 1. "IN" を含むIO候補を全て取得する。見つからない場合はサービスがエラーを報告する。
            var valveCandidates = _ioAddressService.GetAddressRange(sensors, valveSearchString, _cylinder.Cylinder.CYNum, _cylinder.Cylinder.Id, errorIfNotFound: true);

            // 2. 見つかった候補を、末尾の番号をキーとする辞書に変換する
            var valveMap = new Dictionary<int, string>();
            foreach (var candidate in valveCandidates)
            {
                if (string.IsNullOrEmpty(candidate.IOName)) continue;

                // IONameの末尾が1-6の数字であるものを抽出
                char lastChar = candidate.IOName.Last();
                if (int.TryParse(lastChar.ToString(), out int valveNum) && valveNum >= 1 && valveNum <= 6)
                {
                    valveMap[valveNum] = candidate.Address!;
                }
            }

            // 2. 共通のラダーロジックを生成
            result.Add(LadderRow.AddLD(_label + (_startNum + 20).ToString()));
            result.Add(LadderRow.AddOR(_label + (_startNum + 1).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(_label + (_startNum + 16).ToString()));

            result.Add(LadderRow.AddLD(_label + (_startNum + 3).ToString()));
            result.Add(LadderRow.AddANI(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(_label + (_startNum + 18).ToString()));
            result.Add(LadderRow.AddORB());
            result.Add(LadderRow.AddOUT(_label + (_startNum + 9).ToString()));

            // 3. ループを使って、IN1～IN6のラダー生成とエラー処理を共通化
            for (int i = 1; i <= 6; i++)
            {
                // 辞書にバルブが存在するか確認
                if (valveMap.TryGetValue(i, out string? valveAddress))
                {
                    // バルブが見つかった場合：ラダーを生成
                    if (i == 1) // IN1 のみ特殊なロジック
                    {
                        if (_speedDevice != null)
                        {
                            result.AddRange(LadderRow.AddLDG(_speedDevice, "K5"));
                            result.AddRange(LadderRow.AddANDN(_speedDevice, "K0"));
                            result.Add(LadderRow.AddOUT(valveAddress));
                        }
                        else
                        {
                            // speedDevice がない場合のエラー
                            _errorAggregator.AddError(new OutputError
                            {
                                RecordName = _cylinder.Cylinder.CYNum ?? "",
                                Message = $"流量バルブIN1のロジック生成に失敗しました。速度制御デバイスが見つかりません。",
                                MnemonicId = (int)MnemonicType.CY,
                                RecordId = _cylinder.Cylinder.Id
                            });
                        }
                    }
                    else // IN2 から IN6 までの共通ロジック
                    {
                        // ラダーアドレスのオフセットを計算
                        // IN2 -> 21, 26 | IN3 -> 22, 27 ...
                        int ldOffset = 19 + i;
                        int orOffset = 24 + i;
                        result.Add(LadderRow.AddLD(_label + (_startNum + ldOffset).ToString()));
                        result.Add(LadderRow.AddOR(_label + (_startNum + orOffset).ToString()));
                        result.Add(LadderRow.AddOUT(valveAddress));
                    }
                }
                else
                {
                    // バルブが見つからなかった場合：エラーを追加
                    _errorAggregator.AddError(new OutputError
                    {
                        RecordName = _cylinder.Cylinder.CYNum ?? "",
                        Message = $"'{_cylinder.Cylinder.CYNum}' の流量バルブIN{i}がIOリストに見つかりませんでした。",
                        MnemonicId = (int)MnemonicType.CY,
                        RecordId = _cylinder.Cylinder.Id
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// 指定された候補リストから、設定に基づいた特定のバルブアドレスを検索します。
        /// </summary>
        /// <param name="valveCandidates">検索対象のIO候補リスト。</param>
        /// <param name="configuredSuffix">Cylinderに設定されているバルブのSuffix(GoまたはBackの値)。</param>
        /// <param name="valveTypeForErrorMessage">エラーメッセージに表示するためのバルブ種別（例：「前進 (Go)」）。</param>
        /// <returns>見つかった場合はアドレス文字列。見つからない、または設定がない場合はnull。</returns>
        private string? FindValveAddress(
            List<IO> valveCandidates,
            string? configuredSuffix,
            string valveTypeForErrorMessage)
        {
            // 1. シリンダにバルブの設定自体が存在するかチェック
            if (string.IsNullOrEmpty(configuredSuffix))
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id,
                    Message = $"シリンダ「{_cylinder.Cylinder.CYNum}」の{valveTypeForErrorMessage}バルブ出力先が設定されていません。",
                    RecordName = _cylinder.Cylinder.CYNum ?? ""
                });
                return null;
            }

            // 2. 候補リストから一致するものを探す
            var foundValve = valveCandidates
                .FirstOrDefault(m => m.IOName != null && m.IOName.EndsWith(configuredSuffix));

            // 3. IOリスト内に見つかったかチェック
            if (foundValve == null)
            {
                _errorAggregator.AddError(new OutputError
                {
                    MnemonicId = (int)MnemonicType.CY,
                    RecordId = _cylinder.Cylinder.Id,
                    Message = $"IOリスト内に、設定された{valveTypeForErrorMessage}バルブ '{configuredSuffix}' が見つかりませんでした。",
                    RecordName = _cylinder.Cylinder.CYNum ?? ""
                });
                return null;
            }

            return foundValve.Address;
        }


    }
}
