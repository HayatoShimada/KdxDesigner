using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    public class MnemonicTimerDevice
    {
        [Key]
        public long? ID { get; set; }
        public int MnemonicId { get; set; }             // Process: 1, ProcessDetail:2, Operation:3, CY:4
        public int RecordId { get; set; }               // MnemonicIdに対応するテーブルのレコードID
        public int? TimerId {  get; set; }           // TimerテーブルのID
        public int? TimerCategoryId { get; set; }       // RecordIdに対応する処理番号
        public string? ProcessTimerDevice { get; set; }    // RecordIdに対応する処理番号のデバイス
        public string? TimerDevice { get; set; }           // 外部タイマのデバイス
        public int? PlcId { get; set; }               // PLCのID
        public int? CycleId { get; set; }                // サイクルID
    }
}
