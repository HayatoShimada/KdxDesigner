// ViewModel: MemoryEditorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KdxDesigner.Data;
using KdxDesigner.Models;

using Microsoft.Win32;

using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace KdxDesigner.ViewModels
{
    public partial class MemoryEditorViewModel : ObservableObject
    {
        private readonly AccessRepository _repository;
        private readonly int _plcId;

        [ObservableProperty]
        private ObservableCollection<Memory> memories = new();

        [ObservableProperty]
        private ObservableCollection<MemoryCategory> categories = new();

        [ObservableProperty]
        private ObservableCollection<MemoryCategory> memoryCategories = new();

        [ObservableProperty]
        private MemoryCategory? selectedMemoryCategory;

        [ObservableProperty]
        private string saveStatusMessage = string.Empty;

        public MemoryEditorViewModel(int plcId)
        {
            _plcId = plcId;
            _repository = new AccessRepository("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=KDX_Designer.accdb;");
            MemoryCategories = new ObservableCollection<MemoryCategory>(_repository.GetMemoryCategories());
            LoadData();
        }



        private void LoadData()
        {
            Memories = new ObservableCollection<Memory>(_repository.GetMemories(_plcId));
            Categories = new ObservableCollection<MemoryCategory>(_repository.GetMemoryCategories());
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            SaveStatusMessage = "保存中...";

            await Task.Run(() =>
            {
                _repository.SaveMemories(Memories.ToList(), msg =>
                {
                    // UIスレッドに戻してメッセージ更新
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        SaveStatusMessage = msg;
                    });
                });
            });

            SaveStatusMessage = "保存完了！";
        }


        [RelayCommand]
        private void Cancel()
        {
            Memories = new();
        }

        [RelayCommand]
        private void ImportCsv()
        {
            var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv" };
            if (dialog.ShowDialog() == true)
            {
                var lines = File.ReadAllLines(dialog.FileName);
                int? categoryId = SelectedMemoryCategory?.ID;

                var imported = lines.Skip(1) // ヘッダーをスキップ
                    .Select(line =>
                    {
                        var cols = line.Split(',');
                        string device;
                        if (cols.ElementAtOrDefault(2) == null || cols.ElementAtOrDefault(2) == "")
                        {
                            if(cols.ElementAtOrDefault(1) != null)
                            {
                                device = cols.ElementAtOrDefault(1);
                            }
                            else
                            {
                                device = "";
                            }
                        }
                        else
                        {
                            device = cols.ElementAtOrDefault(1) + "." + cols.ElementAtOrDefault(2);
                        }

                        return new Memory
                        {
                            PlcId = _plcId,
                            MemoryCategory = categoryId,
                            DeviceNumber = TryParseInt(cols.ElementAtOrDefault(0)),
                            DeviceNumber1 = cols.ElementAtOrDefault(1),
                            DeviceNumber2 = cols.ElementAtOrDefault(2),
                            Device = device,
                            Category = cols.ElementAtOrDefault(3),
                            Row_1 = cols.ElementAtOrDefault(4),
                            Row_2 = cols.ElementAtOrDefault(5),
                            Row_3 = cols.ElementAtOrDefault(6),
                            Row_4 = cols.ElementAtOrDefault(7),
                            Direct_Input = cols.ElementAtOrDefault(8),
                            Confirm = cols.ElementAtOrDefault(9),
                            Note = cols.ElementAtOrDefault(10),
                            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                    }).ToList();

                foreach (var mem in imported)
                {
                    Memories.Add(mem);
                }
            }
        }



        private int? TryParseInt(string? input)
        {
            return int.TryParse(input, out var result) ? result : null;
        }



        [RelayCommand]
        private void PasteFromClipboard()
        {
            if (!Clipboard.ContainsText()) return;
            var lines = Clipboard.GetText().Split('\n');
            foreach (var line in lines)
            {
                var cols = line.Split('\t'); // Excel形式を想定
                if (cols.Length < 2) continue;

                var mem = new Memory
                {
                    PlcId = _plcId,
                    MemoryCategory = int.TryParse(cols[0], out var mc) ? mc : null,
                    DeviceNumber1 = cols.ElementAtOrDefault(0),
                    DeviceNumber2 = cols.ElementAtOrDefault(1),

                    Row_1 = cols.ElementAtOrDefault(2),
                    Row_2 = cols.ElementAtOrDefault(3),
                    Row_3 = cols.ElementAtOrDefault(4),
                    Row_4 = cols.ElementAtOrDefault(5),
                    Direct_Input = cols.ElementAtOrDefault(6),
                    Confirm = cols.ElementAtOrDefault(7),
                    Note = cols.ElementAtOrDefault(8)
                };
                Memories.Add(mem);
            }
        }
    }
}
