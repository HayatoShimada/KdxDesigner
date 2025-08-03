using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KdxDesigner.Models
{
    /// <summary>
    /// OperationとIOの関連付けを管理する中間テーブル
    /// </summary>
    [Table("OperationIO")]
    public class OperationIO
    {
        [Key]
        [Column(Order = 0)]
        public int OperationId { get; set; }

        [Key]
        [Column(Order = 1)]
        public string IOAddress { get; set; } = string.Empty;

        [Key]
        [Column(Order = 2)]
        public int PlcId { get; set; }

        /// <summary>
        /// IOの用途（Input/Output/Condition/Interlockなど）
        /// </summary>
        public string IOUsage { get; set; } = string.Empty;

        /// <summary>
        /// 表示順序
        /// </summary>
        public int? SortOrder { get; set; }

        /// <summary>
        /// コメント
        /// </summary>
        public string? Comment { get; set; }
    }
}