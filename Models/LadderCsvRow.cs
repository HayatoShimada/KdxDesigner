using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    public class LadderCsvRow
    {
        public string StepNo { get; set; }
        public string StepComment { get; set; }
        public string Command { get; set; }
        public string Address { get; set; }
        public string Blank1 { get; set; }
        public string PiStatement { get; set; }
        public string Note { get; set; }

        public LadderCsvRow()
        {
            // 空欄列を初期化
            StepNo = "\"\"";
            StepComment = "\"\"";
            Command = "\"\"";
            Address = "\"\"";
            Blank1 = "\"\"";
            PiStatement = "\"\"";
            Note = "\"\"";
        }
    }
}

