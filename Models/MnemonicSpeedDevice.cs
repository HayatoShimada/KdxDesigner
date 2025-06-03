using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    public class MnemonicSpeedDevice
    {
        [Key]
        public int ID { get; set; }
        public int CylinderId { get; set; } // Process: 1, ProcessDetail:2, Operation:3
        public string Device { get; set; } = "D0";
        public int PlcId { get; set; }

    }
}
