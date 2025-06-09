using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Utils.MnemonicCommon;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Utils.Cylinder
{
    internal class Common
    {

        public static List<LadderCsvRow> GoOperation(
            List<MnemonicDeviceWithOperation> goOperation,
            List<MnemonicDeviceWithOperation> activeOperation,
            MnemonicDeviceWithCylinder cylinder

            )
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

        public static List<LadderCsvRow> BackOperation(
            List<MnemonicDeviceWithOperation> backOperation,
            MnemonicDeviceWithCylinder cylinder

            )
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

        public static List<LadderCsvRow> GoManualOperation(
            List<MnemonicDeviceWithOperation> goOperation,
            List<MnemonicDeviceWithOperation> activeOperation,
            MnemonicDeviceWithCylinder cylinder

            )
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

        public static List<LadderCsvRow> BackManualOperation(
            List<MnemonicDeviceWithOperation> backOperation,
            MnemonicDeviceWithCylinder cylinder

            )
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

    }
}
