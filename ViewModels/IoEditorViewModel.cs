using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KdxDesigner.Models;
using KdxDesigner.Services;
using KdxDesigner.Services.Access;

using Microsoft.Win32;

using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
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
        private readonly LinkDeviceService _linkDeviceService;

        /// <summary>
        /// 全文検索テキストボックスにバインドされるプロパティ。
        /// </summary>
        [ObservableProperty]
        private string? _fullTextSearch;

        public IoEditorViewModel(IAccessRepository repository)
        {
            // データベースから全てのIOレコードを読み込む
            _allIoRecords = repository.GetIoList();
            _linkDeviceService = new LinkDeviceService(repository);
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

        [RelayCommand]
        private void ExportLinkDeviceCsv()
        {
            // 1. ユーザーに保存場所を選んでもらう
            var dialog = new SaveFileDialog
            {
                Filter = "CSVファイル (*.csv)|*.csv",
                Title = "リンクデバイスCSVを保存",
                FileName = "LinkDeviceList.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // 2. Serviceのメソッドを呼び出してCSVを生成・保存
                    _linkDeviceService.ExportLinkDeviceCsv(dialog.FileName);
                    MessageBox.Show($"CSVファイルを出力しました。\nパス: {dialog.FileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"CSVの出力中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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