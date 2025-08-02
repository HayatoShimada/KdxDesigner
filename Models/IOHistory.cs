using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KdxDesigner.Models
{
    [Table("IOHistory")]
    public class IOHistory
    {
        [Key]
        public int Id { get; set; }
        public string? IoAddress { get; set; } // 変更されたIOのアドレス
        public int? IoPlcId { get; set; } // 変更されたIOのPLC ID
        public string? PropertyName { get; set; } // 変更されたプロパティ名
        public string? OldValue { get; set; } // 変更前の値
        public string? NewValue { get; set; } // 変更後の値
        public string? ChangedAt { get; set; } // 変更後の値

        public string? ChangedBy { get; set; } // 変更者（今回は固定値でもOK）
    }
}