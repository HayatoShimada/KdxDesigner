using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("CY")]
    public class CY
    {
        [Key]
        public int Id { get; set; }
        public int PlcId { get; set; }
        public string? PUCO { get; set; }
        public string? CYNum { get; set; }
        public string? OilNum { get; set; }
        public int? MacineId { get; set; }
        public int? DriveSub { get; set; }
        public int? PlaceId { get; set; }
        public int? CYNameSub { get; set; }
        public int? SensorId { get; set; }
        public string? FlowType { get; set; }
    }

}

