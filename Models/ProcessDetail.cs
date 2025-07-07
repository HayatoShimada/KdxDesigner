using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("ProcessDetail")]
    public class ProcessDetail
    {
        [Key]
        public int Id { get; set; }
        public int ProcessId { get; set; }
        public int? OperationId { get; set; }
        public string? DetailName { get; set; }
        public string? StartIds { get; set; }   // 複数値→カンマ区切り
        public string? FinishIds { get; set; }  // 複数値→カンマ区切り
        public string? StartSensor { get; set; }
        public int? CategoryId { get; set; }
        public string? FinishSensor { get; set; }
        public int? BlockNumber { get; set; }
        public int? TimerId { get; set; }
        public string? SkipMode { get; set; }
        public int? CycleId { get; set; }
        public int? SortNumber { get; set; }
    }

}

