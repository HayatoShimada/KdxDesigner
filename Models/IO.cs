using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KdxDesigner.Models
{
    [Table("IO")]
    public class IO
    {
        public string? IOText { get; set; }
        public string? XComment { get; set; }
        public string? YComment { get; set; }
        public string? FComment { get; set; }
        [Key]
        [Column(Order = 0)]
        public string Address { get; set; } = string.Empty;
        
        [Key]
        [Column(Order = 1)]
        public int PlcId { get; set; }
        
        public string? IOName { get; set; }
        public string? IOExplanation { get; set; }
        public string? IOSpot { get; set; }
        public string? UnitName { get; set; }
        public string? System { get; set; }
        public string? StationNumber { get; set; }
        public string? IONameNaked { get; set; }
        public string? LinkDevice { get; set; }
    }
}
