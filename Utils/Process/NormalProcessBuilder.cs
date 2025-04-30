using KdxDesigner.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Utils.Process
{
    internal class NormalProcessBuilder
    {
        // 通常工程を出力するメソッド
        public static List<LadderCsvRow> BuildNormalPattern(ProcessDetailDto process, List<IO> ioList)
        {
            var result = new List<LadderCsvRow>();
            var startIdList = process.GetStartIdList();
            var finishIdList = process.GetFinishIdList();
            bool firstFlag = true;

            // L***0 ~ L***9のDeviceリスト
            List<string> lDevices = ProcessDetailExtensions.ConvertIdToAddressRange(process.Id);

            // 行間ステートメント

            if (startIdList != null)
            {

            }
            else
            {
                foreach (var row in startIdList)
                {
                    // 初回のみLD
                    result.Add(firstFlag ? LadderRow.AddLD(row) : LadderRow.AddAND(row));
                    firstFlag = false;
                }
            }
            firstFlag = true;
            result.Add(LadderRow.AddOUT(lDevices[0]));

            return result;
        }
    }
}
