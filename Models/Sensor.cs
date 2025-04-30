using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("Sensor")]
    public class Sensor
    {
        [Key]
        public int Id { get; set; }
        public string? SensorName { get; set; }
    }
    
}

