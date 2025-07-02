using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KdxDesigner.Models
{
    [Table("CY")]
    public class OperationBlock
    {
        [Key]
        public int Id { get; set; }
        public string? OperationBlockName { get; set; }


    }

}

