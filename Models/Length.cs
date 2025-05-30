using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("Length")]
    public class Length
    {
        [Key]
        public int ID { get; set; }
        public int PlcId { get; set; }
        public string? LengthName { get; set; }
        public string? Device { get; set; }
    }

}

