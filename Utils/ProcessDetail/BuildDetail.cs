using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Utils.MnemonicCommon;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

// ProcessDetail（工程プログラム）のニモニック配列を返すコード群

namespace KdxDesigner.Utils.ProcessDetail
{
    internal class BuildDetail
    {
        // 通常工程を出力するメソッド
        public static List<LadderCsvRow> BuildDetailNormal(
            MnemonicDeviceWithProcessDetail process,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            errors = new List<OutputError>();                   // エラーリストの初期化
            var result = new List<LadderCsvRow>();

            // L***0 ~ L***9のDeviceリストを取得
            return result;
            
        }

        // 工程まとめを出力するメソッド
        public static List<LadderCsvRow> BuildDetailSummarize(
            MnemonicDeviceWithProcessDetail process,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList,
            out List<OutputError> errors)
        {
            errors = new List<OutputError>();                   // エラーリストの初期化
            var result = new List<LadderCsvRow>();

            // L***0 ~ L***9のDeviceリストを取得
            return result;

        }
    }
}
