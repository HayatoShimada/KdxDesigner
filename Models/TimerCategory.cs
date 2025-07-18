using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KdxDesigner.Models
{
    [Table("TimerCategory")]
    public class TimerCategory
    {
        [Key]
        public int ID { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryFig { get; set; }

    }
}

