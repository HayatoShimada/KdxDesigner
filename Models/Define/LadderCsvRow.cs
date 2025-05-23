using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models.Define
{
    public class LadderCsvRow
    {
        private static int _currentKey = 0;

        public int Key { get; private set; }
        public string StepNo { get; set; }          // ステップ番号
        public string StepComment { get; set; }     // 行間ステートメント
        public string Command { get; set; }         // 命令
        public string Address { get; set; }         // I/O(デバイス)
        public string Blank1 { get; set; }          // 空欄
        public string PiStatement { get; set; }     // PIステートメント
        public string Note { get; set; }            // ノート

        public LadderCsvRow()
        {
            // 空欄列を初期化
            Key = _currentKey++;
            StepNo = "\"\"";
            StepComment = "\"\"";
            Command = "\"\"";
            Address = "\"\"";
            Blank1 = "\"\"";
            PiStatement = "\"\"";
            Note = "\"\"";
        }

        // 任意でカウンターをリセットできるようにする（必要に応じて）
        public static void ResetKeyCounter()
        {
            _currentKey = 0;
        }
    }
}

