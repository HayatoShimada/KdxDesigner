using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KdxDesigner.Models
{
    [Table("CY")]
    public class CY
    {
        [Key]
        public int Id { get; set; }
        public int PlcId { get; set; }
        public string? PUCO { get; set; }
        public string CYNum { get; set; } = string.Empty;
        public string? Go { get; set; }
        public string? Back { get; set; }
        public string? OilNum { get; set; }
        public int? MacineId { get; set; }
        public int? DriveSub { get; set; }
        public int? PlaceId { get; set; }
        public int? CYNameSub { get; set; }
        public int? SensorId { get; set; }
        public string? FlowType { get; set; }
        public string? ProcessStartCycle { get; set; }
        public int? GoSensorCount { get; set; }
        public int? BackSensorCount { get; set; }
        public string? RetentionSensorGo { get; set; }
        public string? RetentionSensorBack { get; set; }
        public int? SortNumber { get; set; }
        public int? Cycle { get; set; }
        public int? FlowCount { get; set; }

    }

}

