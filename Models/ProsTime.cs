using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("ProsTime")]
    public class ProsTime
    {
        [Key]
        public int ID { get; set; }
        public int? PlcId { get; set; }
        public int? MnemonicId { get; set; }
        public int? RecordId { get; set; }
        public int? SortId { get; set; }
        public string? CurrentDevice { get; set; }
        public string? PreviousDevice { get; set; }
        public string? CylinderDevice { get; set; }

    }
}

