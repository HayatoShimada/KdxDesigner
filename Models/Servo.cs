using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("Servo")]

    class Servo
    {
        [Key]
        public int ID { get; set; }
        public int PlcId { get; set; }
        public int CylinderId { get; set; }
        public string Busy { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public int AxisNumber { get; set; }
        public string AxisStop { get; set; } = string.Empty;
        public string PositioningStartNum { get; set; } = string.Empty;
        public string GS { get; set; } = string.Empty;
        public string JogSpeed { get; set; } = string.Empty;
        public string StartFowardJog { get; set; } = string.Empty;
        public string StartReverseJog { get; set; } = string.Empty;
        public string CommandPosition { get; set; } = string.Empty;
        public string CurrentValue { get; set; } = string.Empty;
    }
}
