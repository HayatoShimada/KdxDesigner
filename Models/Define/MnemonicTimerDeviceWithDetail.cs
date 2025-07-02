using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models.Define
{
    public class MnemonicTimerDeviceWithDetail
    {
        public MnemonicTimerDevice Timer { get; set; } = default!;
        public ProcessDetail Detail { get; set; } = default!;

    }
}
