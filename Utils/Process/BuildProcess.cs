using KdxDesigner.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace KdxDesigner.Utils.Process
{
    internal class BuildProcess
    {

        // ProcessCategoryがNormal
        public static List<LadderCsvRow> BuildNormal(
            MnemonicDeviceWithProcess process, 
            List<MnemonicDeviceWithProcessDetail> detail)
        {
            var result = new List<LadderCsvRow>();

            if (process == null)
            {
            }
            else
            {
                // 行間ステートメント
                if (process.Process.ProcessName == null)
                {
                    string id = process.Process.Id.ToString();
                    result.Add(LadderRow.AddStatement(id));
                }
                else
                {
                    string id = process.Process.Id.ToString();
                    result.Add(LadderRow.AddStatement(id + ":" + process.Process.ProcessName));
                }

                // L0 開始条件
                // 開始条件のリストを作る
                List<string> startCondition = process.Process.Autocondition != null
                             ? process.Process.Autocondition.Split(";").ToList()
                             : new List<string>();

                if (startCondition.Count == 0)
                {
                    startCondition.Add("L" + process.Process.TestStart);
                }
                else
                {
                    foreach (var condition in startCondition)
                    {
                        var lDevices = "L" + condition ;

                    }
                }

                // result.Add(LadderRow.AddAND(row));
                // result.Add(LadderRow.AddOUT(lDevices[0]));

                // OUT L1 開始
                // OUT L2 実行中
                // OUT L3 完了条件
                // OUT L4 完了

            }
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
