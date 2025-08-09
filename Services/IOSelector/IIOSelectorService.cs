using KdxDesigner.Models;

namespace KdxDesigner.Services.IOSelector
{
    /// <summary>
    /// 複数のIO候補からユーザーに一つを選択させるUI対話機能を提供します。
    /// </summary>
    public interface IIOSelectorService
    {
        /// <summary>
        /// 選択ダイアログを表示し、ユーザーが選択したIOオブジェクトを返します。
        /// </summary>
        /// <param name="ioText">検索に使われた元のテキスト。</param>
        /// <param name="candidates">ユーザーに提示するIO候補のリスト。</param>
        /// <param name="recordName">コンテキスト情報として表示するレコード名。</param>
        /// <param name="recordId">コンテキスト情報として表示するレコードID。</param>
        /// <returns>ユーザーが選択したIOオブジェクト。選択がキャンセルされた場合はnull。</returns>
        IO? SelectIoFromMultiple(string ioText, List<IO> candidates, string recordName, int? recordId);
    }
}