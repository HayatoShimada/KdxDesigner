using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("Model")]
    public class Model
    {
        [Key]
        public int Id { get; set; }
        public string? ModelName { get; set; }
        public int? CompanyId { get; set; }
    }
    
}

