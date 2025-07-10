using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KdxDesigner.Models;
using KdxDesigner.Services;
using KdxDesigner.Services.Access;

using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace KdxDesigner.ViewModels
{
    public partial class IoEditorViewModel : ObservableObject
    {
        private readonly IAccessRepository _repository;
        private readonly LinkDeviceService _linkDeviceService;

        /// <summary>
        /// データベースから読み込んだ全てのIOレコードをラップしたViewModelのリスト。
        /// </summary>
        private readonly List<IOViewModel> _allIoRecords;

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
            _repository = repository;
            _linkDeviceService = new LinkDeviceService(_repository);

            // ★ IOをIOViewModelでラップしてリストを作成
            _allIoRecords = repository.GetIoList().Select(io => new IOViewModel(io)).ToList();

            IoRecordsView = CollectionViewSource.GetDefaultView(_allIoRecords);
            IoRecordsView.Filter = FilterIoRecord;
        }

        partial void OnFullTextSearchChanged(string? value)
        {
            IoRecordsView.Refresh();
        }

        private bool FilterIoRecord(object item)
        {
            if (string.IsNullOrWhiteSpace(FullTextSearch))
            {
                return true;
            }

            // ★★★ 修正箇所: itemをIOViewModelとして扱う ★★★
            if (item is IOViewModel ioVm)
            {
                string searchTerm = FullTextSearch.ToLower();
                // IOViewModelのプロパティを検索
                return (ioVm.IOText?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.XComment?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.YComment?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.FComment?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.Address?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.IOName?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.IOExplanation?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.IOSpot?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.UnitName?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.System?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.StationNumber?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.IONameNaked?.ToLower().Contains(searchTerm) ?? false) ||
                       (ioVm.LinkDevice?.ToLower().Contains(searchTerm) ?? false);
            }

            return false;
        }

        [RelayCommand]
        private void ExportLinkDeviceCsv()
        {
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
                    // ★★★ 修正箇所: List<IOViewModel> を List<IO> に変換 ★★★
                    _linkDeviceService.ExportLinkDeviceCsv(dialog.FileName);

                    MessageBox.Show($"CSVファイルを出力しました。\nパス: {dialog.FileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"CSVの出力中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void SaveChanges()
        {
            try
            {
                // ★★★ 修正箇所: IsDirtyでフィルタリング ★★★
                var changedVms = _allIoRecords.Where(vm => vm.IsDirty).ToList();
                if (!changedVms.Any())
                {
                    MessageBox.Show("変更された項目はありません。", "情報");
                    return;
                }

                var histories = new List<IOHistory>();
                var ioToUpdate = new List<IO>();

                // ★★★ 修正箇所: 変数名を統一 ★★★
                var changedIds = changedVms.Select(vm => vm.Id).ToHashSet();
                var originalIos = _repository.GetIoList().Where(io => changedIds.Contains(io.Id))
                                             .ToDictionary(io => io.Id);

                foreach (var changedVm in changedVms)
                {
                    var updatedIo = changedVm.GetModel();
                    ioToUpdate.Add(updatedIo);

                    if (!originalIos.TryGetValue(changedVm.Id, out var originalIo)) continue;

                    var properties = typeof(IO).GetProperties();
                    foreach (var prop in properties)
                    {
                        if (prop.Name == "Id") continue;

                        var oldValue = prop.GetValue(originalIo);
                        var newValue = prop.GetValue(updatedIo);
                        var oldValueStr = oldValue?.ToString() ?? "";
                        var newValueStr = newValue?.ToString() ?? "";

                        if (oldValueStr != newValueStr)
                        {
                            histories.Add(new IOHistory
                            {
                                IoId = updatedIo.Id,
                                PropertyName = prop.Name,
                                OldValue = oldValueStr,
                                NewValue = newValueStr,
                                ChangedAt = DateTime.Now.ToString(),
                                ChangedBy = "user"
                            });
                        }
                    }
                }

                // ★★★ 修正箇所: 正しいメソッドを呼び出す ★★★
                if (ioToUpdate.Any())
                {
                    _repository.UpdateAndLogIoChanges(ioToUpdate, histories);
                }

                changedVms.ForEach(vm => vm.IsDirty = false);

                MessageBox.Show($"{changedVms.Count}件の変更を保存しました。", "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存中にエラーが発生しました: {ex.Message}", "エラー");
            }
        }
    }
}