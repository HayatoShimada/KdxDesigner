using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services.Error;
using KdxDesigner.Utils.MnemonicCommon;
using KdxDesigner.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Utils.Cylinder
{
    internal class CylinderFunction
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IErrorAggregator _errorAggregator;

        // コンストラクタでMainViewModelをインジェクト
        public CylinderFunction(MainViewModel mainViewModel, IErrorAggregator errorAggregator)
        {
            _mainViewModel = mainViewModel;
            _errorAggregator = errorAggregator;
        }

        public List<LadderCsvRow> GoOperation(
            List<MnemonicDeviceWithOperation> goOperation,
            List<MnemonicDeviceWithOperation> activeOperation,
            MnemonicDeviceWithCylinder cylinder)
        {
            List<LadderCsvRow> result = new List<LadderCsvRow>(); // 生成されるLadderCsvRowのリスト
            bool isFirst = true; // 最初のOperationかどうかのフラグ

            string label = cylinder.Mnemonic.DeviceLabel ?? ""; // ラベルの取得
            int startNum = cylinder.Mnemonic.StartNum ?? 0; // ラベルの取得

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

            result.Add(LadderRow.AddOUT(label + (startNum + 0).ToString())); // ラベルのLD命令を追加

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> BackOperation(
            List<MnemonicDeviceWithOperation> backOperation,
            MnemonicDeviceWithCylinder cylinder)
        {
            List<LadderCsvRow> result = new List<LadderCsvRow>(); // 生成されるLadderCsvRowのリスト
            bool isFirst = true; // 最初のOperationかどうかのフラグ

            string label = cylinder.Mnemonic.DeviceLabel ?? ""; // ラベルの取得
            int startNum = cylinder.Mnemonic.StartNum ?? 0; // ラベルの取得

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
            result.Add(LadderRow.AddOUT(label + (startNum + 0).ToString())); // ラベルのLD命令を追加
            result.Add(LadderRow.AddOUT(label + (startNum + 0).ToString())); // ラベルのLD命令を追加

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> GoManualOperation(
            List<MnemonicDeviceWithOperation> goOperation,
            List<MnemonicDeviceWithOperation> activeOperation,
            MnemonicDeviceWithCylinder cylinder)
        {
            List<LadderCsvRow> result = new List<LadderCsvRow>(); // 生成されるLadderCsvRowのリスト
            bool isFirst = true; // 最初のOperationかどうかのフラグ

            string label = cylinder.Mnemonic.DeviceLabel ?? ""; // ラベルの取得
            int startNum = cylinder.Mnemonic.StartNum ?? 0; // ラベルの取得

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

            result.Add(LadderRow.AddOUT(label + (startNum + 0).ToString())); // ラベルのLD命令を追加

            return result; // 生成されたLadderCsvRowのリストを返す
        }

        public List<LadderCsvRow> BackManualOperation(
            List<MnemonicDeviceWithOperation> backOperation,
            MnemonicDeviceWithCylinder cylinder)
        {
            List<LadderCsvRow> result = new List<LadderCsvRow>(); // 生成されるLadderCsvRowのリスト
            bool isFirst = true; // 最初のOperationかどうかのフラグ

            string label = cylinder.Mnemonic.DeviceLabel ?? ""; // ラベルの取得
            int startNum = cylinder.Mnemonic.StartNum ?? 0; // ラベルの取得

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
            result.Add(LadderRow.AddOUT(label + (startNum + 0).ToString())); // ラベルのLD命令を追加
            result.Add(LadderRow.AddOUT(label + (startNum + 0).ToString())); // ラベルのLD命令を追加

            return result; // 生成されたLadderCsvRowのリストを返す
        }


        public List<LadderCsvRow> CyclePulse(
            MnemonicDeviceWithCylinder cylinder,
            List<Cycle> cycles)
        {
            List<LadderCsvRow> result = new List<LadderCsvRow>(); // 生成されるLadderCsvRowのリスト
            bool isFirst = true; // 最初のOperationかどうかのフラグ

            string label = cylinder.Mnemonic.DeviceLabel ?? ""; // ラベルの取得
            int startNum = cylinder.Mnemonic.StartNum ?? 0; // ラベルの取得

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

            return result; // 生成されたLadderCsvRowのリストを返す
        }




    }
}
