using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KdxDesigner.Models
{
    [Table("ProcessDetailConnection")]
    public class ProcessDetailConnection
    {
        [Key]
        public int Id { get; set; }
        
        public int FromProcessDetailId { get; set; }
        
        public int ToProcessDetailId { get; set; }
        
        public string? StartSensor { get; set; }
        
        // ナビゲーションプロパティ（必要に応じて）
        [ForeignKey("FromProcessDetailId")]
        public virtual ProcessDetail? FromProcessDetail { get; set; }
        
        [ForeignKey("ToProcessDetailId")]
        public virtual ProcessDetail? ToProcessDetail { get; set; }
    }
}