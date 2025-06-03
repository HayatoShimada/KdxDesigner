using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KdxDesigner.Models
{
    [Table("OperationDifinisions")]
    public class OperationDifinisions
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public string CategoryName { get; set; } = string.Empty;
        [Required]
        public int OutCoilNumber { get; set; }
        public string? Description { get; set; }

    }
}
