using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("Error")]
    public class Error
    {
        [Key]
        public int ID { get; set; }
        public int? PlcId { get; set; }
        public int? CycleId { get; set; }
        public string? Device { get; set; }
        public int? MnemonicId { get; set; }
        public int? RecordId { get; set; }
        public int? AlarmId { get; set; }
        public int? ErrorNum { get; set; }
        public string? AlarmComment { get; set; }
        public string? MessageComment { get; set; }
        public int? ErrorTime { get; set; }
        public string? ErrorTimeDevice { get; set; }
    }
}

