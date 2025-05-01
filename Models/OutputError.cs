using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    public class OutputError
    {
        public string? DetailName { get; set; }   // 工程名
        public string? Message { get; set; }      // エラーメッセージ
        public int? ProcessId { get; set; }       // 対象プロセスID
    }
}
