using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KdxDesigner.Models
{
    [Table("ProcessDetailFinish")]
    public class ProcessDetailFinish
    {
        [Key]
        public int Id { get; set; }
        
        public int ProcessDetailId { get; set; }
        
        public int FinishProcessDetailId { get; set; }
        
        // ナビゲーションプロパティ（必要に応じて）
        [ForeignKey("ProcessDetailId")]
        public virtual ProcessDetail? ProcessDetail { get; set; }
        
        [ForeignKey("FinishProcessDetailId")]
        public virtual ProcessDetail? FinishProcessDetail { get; set; }
    }
}