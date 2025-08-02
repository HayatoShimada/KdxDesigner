using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KdxDesigner.Models
{
    /// <summary>
    /// CYテーブルとIOテーブルを関連付ける中間テーブル
    /// </summary>
    [Table("CylinderIO")]
    public class CylinderIO
    {
        [Key]
        [Column(Order = 0)]
        public int CylinderId { get; set; }  // CYテーブルのId
        
        [Key]
        [Column(Order = 1)]
        public string IOAddress { get; set; } = string.Empty;  // IOテーブルのAddress
        
        [Key]
        [Column(Order = 2)]
        public int PlcId { get; set; }  // IOテーブルのPlcId（複合キーの一部）
        
        /// <summary>
        /// IOの用途タイプ
        /// </summary>
        public string IOType { get; set; } = string.Empty;  // "GoSensor", "BackSensor", "GoValve", "BackValve" など
        
        /// <summary>
        /// 表示順序
        /// </summary>
        public int? SortOrder { get; set; }
        
        /// <summary>
        /// 備考
        /// </summary>
        public string? Comment { get; set; }
    }
}