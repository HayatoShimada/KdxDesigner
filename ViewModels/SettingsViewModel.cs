﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KdxDesigner.Models.Define;
using KdxDesigner.Services;
using KdxDesigner.Utils;

using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace KdxDesigner.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly DatabasePathManager _pathManager;

        [ObservableProperty]
        private ObservableCollection<SettingItem> _settingItems = new();

        [ObservableProperty]
        private string? _databasePath;

        private readonly Window _window;

        public SettingsViewModel(Window window)
        {
            _window = window;
            _pathManager = new DatabasePathManager();

            // 現在の設定を読み込んで表示
            DatabasePath = _pathManager.CurrentDatabasePath;
            LoadSettings();
        }

        private void LoadSettings()
        {
            var props = typeof(AppSettings).GetProperties();
            SettingItems = new ObservableCollection<SettingItem>(
                props.Select(p => new SettingItem
                {
                    Key = p.Name,
                    Value = p.GetValue(SettingsManager.Settings)?.ToString() ?? "",
                    Description = p.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
                                   .Cast<System.ComponentModel.DescriptionAttribute>()
                                   .FirstOrDefault()?.Description ?? ""
                })
            );
        }

        /// <summary>
        /// ★【新規】データベースパスの変更ダイアログを開くコマンド
        /// </summary>
        [RelayCommand]
        private void ChangeDatabasePath()
        {
            // ★ SelectAndSaveNewPathメソッドを呼び出す
            if (_pathManager.SelectAndSaveNewPath())
            {
                // パスが正常に選択・保存されたら、UIの表示も更新
                DatabasePath = _pathManager.CurrentDatabasePath;

                MessageBox.Show(
                    "データベースのパスが変更されました。\nアプリケーションを再起動して設定を反映してください。",
                    "情報",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            // キャンセルされた場合は何もしない
        }

        /// <summary>
        /// DataGrid内の設定を保存するコマンド
        /// </summary>
        [RelayCommand]
        private void Save()
        {
            var settingsObj = SettingsManager.Settings;
            var settingsType = typeof(AppSettings);

            foreach (var item in SettingItems)
            {
                if (string.IsNullOrEmpty(item.Key)) continue;

                var prop = settingsType.GetProperty(item.Key);
                if (prop == null || !prop.CanWrite) continue;

                try
                {
                    // 型変換をより安全に行う
                    var convertedValue = System.Convert.ChangeType(item.Value, prop.PropertyType);
                    prop.SetValue(settingsObj, convertedValue);
                }
                catch
                {
                    MessageBox.Show($"設定 [{item.Key}] の値「{item.Value}」は正しい形式ではありません。", "保存エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return; // エラーがあれば中断
                }
            }

            SettingsManager.Save();
            MessageBox.Show("設定を保存しました！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }



        [RelayCommand]
        private void Close()
        {
            _window.Close();
        }
    }

    public class SettingItem
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
        public string? Description { get; set; }
    }
}