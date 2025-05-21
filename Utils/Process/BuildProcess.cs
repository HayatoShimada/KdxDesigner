using KdxDesigner.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Utils.Process
{
    internal class BuildProcess
    {

        // ProcessCategoryがNormal
        // process:Processテーブルのレコード
        // processStartNum:ProcessプログラムのLデバイスのスタート番号
        // detailStartNum:ProcessDetailプログラムのLデバイスのスタート番号
        public static List<LadderCsvRow> BuildNormal(Models.Process process, int processStartNum, int detailStartNum)
        {
            var result = new List<LadderCsvRow>();

            // コードを記述
            if (process == null)
            {
            }
            else
            {
                // 行間ステートメント
                if (process.ProcessName == null)
                {
                    string id = process.Id.ToString();
                    result.Add(LadderRow.AddStatement(id));
                }
                else
                {
                    string id = process.Id.ToString();
                    result.Add(LadderRow.AddStatement(id + ":" + process.ProcessName));
                }

                // L0 Condition


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
