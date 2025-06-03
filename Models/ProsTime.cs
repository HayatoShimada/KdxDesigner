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
        [Required]
        public int PlcId { get; set; }
        [Required]
        public int MnemonicId { get; set; }
        [Required]
        public int RecordId { get; set; }
        [Required]
        public int SortId { get; set; }
        [Required]
        public string CurrentDevice { get; set; } = "ZR0";
        [Required]
        public string PreviousDevice { get; set; } = "ZR0";
        [Required]
        public string CylinderDevice { get; set; } = "ZR0";
        [Required]
        public int CategoryId { get; set; } // OperationDifinitionsのID

    }
}

