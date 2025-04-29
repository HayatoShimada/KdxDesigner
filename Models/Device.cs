using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("Device")]
    public class Device
    {
        [Key]
        public int Id { get; set; }
        public string? DeviceName { get; set; }
        public int? ModelId { get; set; }
    }
    
}

