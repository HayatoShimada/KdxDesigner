using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("ProsTimeCategory")]
    public class ProsTimeDefinitions
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int OperationCategoryId { get; set; }
        [Required]
        public int TotalCount { get; set; }
        [Required]
        public int SortOrder { get; set; }
        [Required]
        public int OperationDifinitionsId { get; set; } // 外部キーとして OperationDifinitions テーブルの ID を参照
    }
    
}

