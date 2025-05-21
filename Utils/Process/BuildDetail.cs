using KdxDesigner.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

// ProcessDetail（工程プログラム）のニモニック配列を返すコード群

namespace KdxDesigner.Utils.Process
{
    internal class BuildDetail
    {
        // 通常工程を出力するメソッド
        public static List<LadderCsvRow> BuildNormalPattern(ProcessDetailDto process, List<IO> ioList)
        {
            var result = new List<LadderCsvRow>();
            var startIdList = process.GetStartIdList();
            var finishIdList = process.GetFinishIdList();

            // L***0 ~ L***9のDeviceリスト
            List<string> lDevices = ProcessDetailExtensions.ConvertIdToAddressRange(process.Id);

            if (process == null)
            {
                return result;
            }
            else
            {
                // 行間ステートメント
                if(process.DetailName == null)
                {
                    string id = process.Id.ToString();
                    result.Add(LadderRow.AddStatement(id));
                }
                else
                {
                    string id = process.Id.ToString();
                    result.Add(LadderRow.AddStatement(id + ":" + process.DetailName));
                }

                // L0 Condition
                if (startIdList != null && startIdList.Count > 0)
                {
                    foreach (var row in startIdList)
                    {
                        result.Add(LadderRow.AddAND(row));
                    }
                }
                result.Add(LadderRow.AddOUT(lDevices[0]));

                // L1 Start
                // L2 
                // L3
                // L4
                // L5
                // L6
                // L7
                // L8
                // L9 Finish

                return result;
            }
        }
    }
}
