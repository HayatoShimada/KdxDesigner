namespace KdxDesigner.Models
{
    public class OutputError
    {
        public string? Message { get; set; }      // エラーメッセージ
        public int? MnemonicId { get; set; }       // 対象ニモニックID
        public int? RecordId { get; set; }       // 対象プロセスID
        public string? RecordName { get; set; }   // 工程名
        public bool IsCritical { get; set; } = false; // 致命的なエラーかどうか
    }
}
