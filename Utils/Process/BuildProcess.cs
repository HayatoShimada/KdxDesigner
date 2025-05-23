using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using KdxDesigner.Utils.MnemonicCommon;
using System.Runtime.ExceptionServices;
using KdxDesigner.Models.Define;

namespace KdxDesigner.Utils.Process
{
    internal class BuildProcess
    {

        // ProcessCategoryがNormal
        // Processのレコード1個とProcessDetail全体を受け取る
        public static List<LadderCsvRow> BuildNormal(
            MnemonicDeviceWithProcess process,
            List<MnemonicDeviceWithProcessDetail> detail)
        {
            var result = new List<LadderCsvRow>();

            // 行間ステートメント
            string id = process.Process.Id.ToString();
            if (string.IsNullOrEmpty(process.Process.ProcessName))
            {
                result.Add(LadderRow.AddStatement(id));
            }
            else
            {
                result.Add(LadderRow.AddStatement(id + ":" + process.Process.ProcessName));
            }

            // L0 開始条件
            // まず開始条件の数値リストを作る
            // process.Process.Autocondition 例) 1;2;3;4;5
            List<int> startCondition = !string.IsNullOrEmpty(process.Process.AutoCondition)
                                                ? process.Process.AutoCondition
                                                    .Split(';')
                                                    .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                                                    .Where(n => n.HasValue)
                                                    .Select(n => n.Value)
                                                    .ToList()
                                                : new List<int>();

            // 初回はLD命令
            var first = true;

            // 開始条件のProcessDetail側の完了接点のアウトコイルを取得する。
            // 基本ルールとして、ProcessDetail側の完了接点は、[FirstNum + OutCoilCount - 1]の数値になる。
            foreach (var condition in startCondition)
            {
                // 1. Processの開始条件のIDから、ProcessDetailのレコードを取得する。
                var target = detail.FirstOrDefault(d => d.Mnemonic.RecordId == condition);

                // エラー処理を追加してください   issue#10
                if (target?.Mnemonic == null)
                    continue;

                // 2. ProcessDetailのレコードから、完了のアウトコイルの数値を取得する。
                var mnemonic = target.Mnemonic;

                // 3. ラベルと数値を取得して結合する。
                var label = mnemonic.DeviceLabel ?? string.Empty;
                var deviceNumber = mnemonic.StartNum + mnemonic.OutCoilCount - 1;
                var device = deviceNumber.ToString();

                var labelDevice = label + device;

                // 4. 命令を生成する
                var row = first
                    ? LadderRow.AddLD(labelDevice)
                    : LadderRow.AddAND(labelDevice);

                result.Add(row);
                first = false;
            }

            // OUT L0 開始条件
            int? outcoilNum = process.Mnemonic.StartNum;
            var outcoilLabel = process.Mnemonic.DeviceLabel ?? string.Empty;
            result.Add(LadderRow.AddOUT(outcoilLabel + outcoilNum.ToString()));



            // OUT L1 開始
            // 試運転スル
            var debugContact = process.Process.TestMode;
            result.Add(LadderRow.AddLDI(debugContact));

            // 試運転実行処理
            var debugStartContact = process.Process.TestStart;
            result.Add(LadderRow.AddLD(debugStartContact));

            var debugCondition = process.Process.TestCondition;
            result.Add(LadderRow.AddAND(debugCondition));

            result.Add(LadderRow.AddAND(debugContact));
            result.Add(LadderRow.AddORB());

            // アウトコイルまで
            result.Add(LadderRow.AddAND(outcoilLabel + outcoilNum.ToString()));
            result.Add(LadderRow.AddANI(outcoilLabel + (outcoilNum + 1).ToString()));
            result.Add(LadderRow.AddOR(outcoilLabel + (outcoilNum + 1).ToString()));

            var startContact = process.Process.AutoStart;
            result.Add(LadderRow.AddAND(startContact));
            result.Add(LadderRow.AddOUT(outcoilLabel + (outcoilNum + 1).ToString()));

            // CJの実装
            result.Add(LadderRow.AddLDP(outcoilLabel + (outcoilNum + 1).ToString()));
            result.Add(LadderRow.AddAND(debugStartContact));
            result.Add(LadderRow.AddCJ("P0"));          // issue#11

            // OUT L2 実行中
            result.Add(LadderRow.AddLD(outcoilLabel + (outcoilNum + 1).ToString()));
            result.Add(LadderRow.AddANI(outcoilLabel + (outcoilNum + 4).ToString()));
            result.Add(LadderRow.AddOUT(outcoilLabel + (outcoilNum + 2).ToString()));

            // OUT L4 完了
            var completeContact = process.Process.FinishId;
            var completeDetailRecord = detail.FirstOrDefault(d => d.Mnemonic.RecordId == completeContact);
            var completeMnemonic = completeDetailRecord.Mnemonic;
            var completeLabel = completeMnemonic.DeviceLabel ?? string.Empty;
            var completeNumber = completeMnemonic.StartNum + completeMnemonic.OutCoilCount - 1;
            var completeDevice = completeNumber.ToString();
            var completeLabelDevice = completeLabel + completeDevice;

            result.Add(LadderRow.AddLD(outcoilLabel + (outcoilNum + 1).ToString()));
            result.Add(LadderRow.AddAND(completeLabelDevice));
            result.Add(LadderRow.AddOUT(outcoilLabel + (outcoilNum + 4).ToString()));

            return result;
        }

        // ProcessCategoryがResetAfter
        public static List<LadderCsvRow> BuildResetAfter(Models.Process process, int processStartNum, int detailStartNum)
        {
            var result = new List<LadderCsvRow>();

            // Normalを参考にコードを記述すること
            // #issue6

            return result;
        }

        // ProcessCategoryがIL
        public static List<LadderCsvRow> BuildIL(Models.Process process, int processStartNum, int detailStartNum)
        {
            var result = new List<LadderCsvRow>();

            // Normalを参考にコードを記述すること
            // #issue7

            return result;
        }

    }
}
