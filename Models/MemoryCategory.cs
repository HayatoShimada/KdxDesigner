using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("MemoryCategory")]
    public class MemoryCategory
    {
        [Key]
        public int ID { get; set; }
        public string? CategoryName { get; set; }
        public string? Enum { get; set; }

    }
}
