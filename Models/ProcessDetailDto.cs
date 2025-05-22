using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    public class ProcessDetailDto
    {
        public int Id { get; set; }
        public int? ProcessId { get; set; }
        public string? ProcessName { get; set; }
        public int? OperationId { get; set; }
        public string? OperationName { get; set; }
        public string? DetailName { get; set; }
        public string? StartIds { get; set; }
        public string? FinishIds { get; set; }
        public string? StartSensor { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }  // ← 追加
        public string? FinishSensor { get; set; }

        public int? CycleId { get; set; }
    }
}
