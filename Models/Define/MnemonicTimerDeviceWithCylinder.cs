using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models.Define
{
    public class MnemonicTimerDeviceWithCylinder
    {
        public MnemonicTimerDevice Timer { get; set; } = default!;
        public CY Cylinder { get; set; } = default!;
        
    }
}
