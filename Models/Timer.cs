using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("Timer")]
    public class Timer
    {
        [Key]
        public int ID { get; set; }
        public int? CycleId { get; set; }
        public int? TimerCategoryId { get; set; }
        public int? TimerNum { get; set; }
        public string? TimerName { get; set; }
        public int? MnemonicId { get; set; }
        public string? RecordIds { get; set; }  // RecordIdから変更 数字のカンマ区切りリスト
        public int? Example { get; set; }
    }
}

