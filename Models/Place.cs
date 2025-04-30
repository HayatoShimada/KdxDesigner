using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("Place")]
    public class Place
    {
        [Key]
        public int Id { get; set; }
        public int? ModelId { get; set; }
        public string? PlaceName { get; set; }
    }
    
}

