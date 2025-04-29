using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("Macine")]
    public class Macine
    {
        [Key]
        public int Id { get; set; }
        public string? MacineName { get; set; }
        public string? ShortName { get; set; }
    }
    
}

