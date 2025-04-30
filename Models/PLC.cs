using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("PLC")]
    public class PLC
    {
        [Key]
        public int Id { get; set; }
        public string? PlcName { get; set; }
        public int? ModelId { get; set; }
        public string? Maker { get; set; }

    }

}

