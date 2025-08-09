using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KdxDesigner.Models
{
    /// <summary>
    /// 工程の開始条件を管理する中間テーブル
    /// ProcessテーブルのAutoConditionフィールド（複数値）を正規化したもの
    /// </summary>
    [Table("ProcessStartCondition")]
    public class ProcessStartCondition
    {
        /// <summary>
        /// ID (主キー)
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 工程ID
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// 開始条件となる工程詳細ID
        /// </summary>
        public int StartProcessDetailId { get; set; }

        /// <summary>
        /// 開始センサー（オプション）
        /// </summary>
        public string? StartSensor { get; set; }

        /// <summary>
        /// ナビゲーションプロパティ: 工程
        /// </summary>
        [ForeignKey("ProcessId")]
        public virtual Process? Process { get; set; }

        /// <summary>
        /// ナビゲーションプロパティ: 開始条件の工程詳細
        /// </summary>
        [ForeignKey("StartProcessDetailId")]
        public virtual ProcessDetail? StartProcessDetail { get; set; }
    }
}