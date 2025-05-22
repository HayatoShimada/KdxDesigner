using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    public class MnemonicDeviceWithProcess
    {
        public MnemonicDevice Mnemonic { get; set; } = default!;
        public Models.Process Process { get; set; } = default!;
        
    }
}
