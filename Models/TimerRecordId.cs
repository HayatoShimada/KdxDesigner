using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KdxDesigner.Models
{
    [Table("TimerRecordIds")]
    public class TimerRecordId
    {
        [Key]
        [Column(Order = 0)]
        public int TimerId { get; set; }

        [Key]
        [Column(Order = 1)]
        public int RecordId { get; set; }

        // ナビゲーションプロパティ
        public virtual Timer? Timer { get; set; }
    }
}