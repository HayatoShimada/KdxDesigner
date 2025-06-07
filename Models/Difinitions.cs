using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KdxDesigner.Models
{
    [Table("Difinisions")]
    public class Difinitions
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public string DefName { get; set; } = string.Empty;
        [Required]
        public int OutCoilNumber { get; set; }
        public string? Description { get; set; }
        public string? Comment1 { get; set; }
        public string? Comment2 { get; set; }
        public string Label { get; set; } = "M";
        public string Category { get; set; } = "Nan";
    }
}
