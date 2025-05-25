using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models.Define
{
    public class MnemonicDeviceWithOperation
    {
        public MnemonicDevice Mnemonic { get; set; } = default!;
        public Operation Operation { get; set; } = default!;
        
    }
}
