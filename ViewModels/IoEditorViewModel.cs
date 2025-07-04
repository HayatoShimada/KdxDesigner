using CommunityToolkit.Mvvm.ComponentModel;

using KdxDesigner.Models;
using KdxDesigner.Services.Access;

using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;

namespace KdxDesigner.ViewModels
{
    public partial class IoEditorViewModel : ObservableObject
    {
        /// <summary>
        /// データベースから読み込んだ全てのIOレコードを保持します。
        /// </summary>
        private readonly List<IO> _allIoRecords;

        /// <summary>
        /// DataGridにバインドするための、フィルタリングとソートが可能なIOレコードのビュー。
        /// </summary>
        public ICollectionView IoRecordsView { get; }

        /// <summary>
        /// 全文検索テキストボックスにバインドされるプロパティ。
        /// </summary>
        [ObservableProperty]
        private string? _fullTextSearch;

        public IoEditorViewModel(IAccessRepository repository)
        {
            // データベースから全てのIOレコードを読み込む
            _allIoRecords = repository.GetIoList();

            // ICollectionViewを初期化し、フィルタリングロジックを割り当てる
            IoRecordsView = CollectionViewSource.GetDefaultView(_allIoRecords);
            IoRecordsView.Filter = FilterIoRecord;
        }

        /// <summary>
        /// 全文検索テキストが変更されたときに呼び出され、フィルタを再適用します。
        /// </summary>
        partial void OnFullTextSearchChanged(string? value)
        {
            IoRecordsView.Refresh();
        }

        /// <summary>
        /// ICollectionViewのフィルタリングロジック。
        /// </summary>
        /// <param name="item">コレクション内の各IOオブジェクト。</param>
        /// <returns>表示する場合はtrue、非表示にする場合はfalse。</returns>
        private bool FilterIoRecord(object item)
        {
            // 検索テキストが空の場合は、全ての項目を表示
            if (string.IsNullOrWhiteSpace(FullTextSearch))
            {
                return true;
            }

            if (item is IO io)
            {
                // 検索テキストを小文字に変換（大文字・小文字を区別しない検索のため）
                string searchTerm = FullTextSearch.ToLower();

                // IOオブジェクトの全てのテキスト系プロパティを対象に、検索テキストが含まれるかチェック
                // (nullでないプロパティのみを対象とする)
                return (io.IOText?.ToLower().Contains(searchTerm) ?? false) ||
                       (io.XComment?.ToLower().Contains(searchTerm) ?? false) ||
                       (io.YComment?.ToLower().Contains(searchTerm) ?? false) ||
                       (io.FComment?.ToLower().Contains(searchTerm) ?? false) ||
                       (io.Address?.ToLower().Contains(searchTerm) ?? false) ||
                       (io.IOName?.ToLower().Contains(searchTerm) ?? false) ||
                       (io.IOExplanation?.ToLower().Contains(searchTerm) ?? false) ||
                       (io.IOSpot?.ToLower().Contains(searchTerm) ?? false) ||
                       (io.UnitName?.ToLower().Contains(searchTerm) ?? false) ||
                       (io.System?.ToLower().Contains(searchTerm) ?? false) ||
                       (io.StationNumber?.ToLower().Contains(searchTerm) ?? false) ||
                       (io.IONameNaked?.ToLower().Contains(searchTerm) ?? false) ||
                       (io.LinkDevice?.ToLower().Contains(searchTerm) ?? false);
            }

            return false;
        }
    }
}