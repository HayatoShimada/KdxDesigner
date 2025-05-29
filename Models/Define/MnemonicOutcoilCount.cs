using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models.Define
{
    public enum MnemonicOutcoilCount
    {
        [Description("プロセス")]
        Process = 5,

        [Description("詳細工程")]
        ProcessDetail = 10,

        [Description("動作")]
        Operation = 20,

        [Description("シリンダ")]
        CY = 200
    }

}
