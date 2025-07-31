using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("ErrorMessage")]
    public class ErrorMessage
    {
        [Key]
        public int MnemonicId { get; set; }
        [Key]
        public int AlarmId { get; set; }
        public string? BaseMessage { get; set; }
        public string? BaseAlarm { get; set; }
        public string? Category1 { get; set; }
        public string? Category2 { get; set; }
        public string? Category3 { get; set; }

        public int DefaultCountTime { get; set; } = 0;
    }
}
