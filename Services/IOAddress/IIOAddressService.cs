using KdxDesigner.Models;

namespace KdxDesigner.Services.IOAddress
{
    /// <summary>
    /// IOリストに対する検索機能を提供します。
    /// エラーは内部で IErrorAggregator を通じて報告されます。
    /// </summary>
    public interface IIOAddressService
    {
        /// <summary>
        /// 指定されたテキストに一致する単一のIOアドレスを取得します。
        /// 検索結果が0件、または複数件の場合はエラーとして報告され、nullを返します。
        /// "L-"プレフィックスを持つテキストも解釈します。
        /// </summary>
        /// <param name="ioList">検索対象のIOリスト。</param>
        /// <param name="ioText">検索するIOテキスト（例: "G", "B", "L-LENGTH1"）。</param>
        /// <param name="plcId">"L-"プレフィックス検索時に使用するPLCのID。</param>
        /// <returns>一意に特定できた場合はIOアドレス文字列。それ以外の場合はnull。</returns>
        string? GetSingleAddress(
            List<IO> ioList,
            string ioText,
            bool isOutput,
            string recordName,
            int? recordId,
            string? isnotInclude);

        /// <summary>
        /// 指定されたテキストを含むIOオブジェクトのリストを取得します。
        /// </summary>
        /// <param name="ioList">検索対象のIOリスト。</param>
        /// <param name="ioText">検索するIOテキスト（例: "SV"）。</param>
        /// <param name="errorIfNotFound">trueの場合、一致するIOが見つからなければエラーとして報告します。</param>
        /// <returns>一致したIOオブジェクトのリスト。見つからない場合は空のリストを返します。</returns>
        List<IO> GetAddressRange(
            List<IO> ioList, 
            string ioText, 
            string processName,
            int recordId,
            bool errorIfNotFound = false);
    }
}