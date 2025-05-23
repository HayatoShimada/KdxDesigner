using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models.Define
{
    public enum MnemonicType
    {
        [Description("プロセス")]
        Process = 1,

        [Description("詳細工程")]
        ProcessDetail = 2,

        [Description("動作")]
        Operation = 3,

        [Description("シリンダ")]
        CY = 4
    }

}
