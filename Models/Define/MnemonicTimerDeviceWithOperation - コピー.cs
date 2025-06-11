using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models.Define
{
    public class MnemonicTimerDeviceWithOperation
    {
        public MnemonicTimerDevice Timer { get; set; } = default!;
        public Operation Operation { get; set; } = default!;
        
    }
}
