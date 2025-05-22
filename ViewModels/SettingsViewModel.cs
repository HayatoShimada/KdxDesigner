using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KdxDesigner.Models;
using KdxDesigner.Utils;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;

namespace KdxDesigner.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<SettingItem> settingItems = new();

        private readonly Window _window;

        public SettingsViewModel(Window window)
        {
            _window = window;
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

        [RelayCommand]
        private void Save()
        {
            var settingsObj = SettingsManager.Settings;
            var settingsType = typeof(AppSettings);

            foreach (var item in SettingItems)
            {
                if (string.IsNullOrEmpty(item.Key)) // Ensure Key is not null or empty
                {
                    MessageBox.Show("設定項目のキーが無効です。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var prop = settingsType.GetProperty(item.Key);
                if (prop == null || !prop.CanWrite) continue;

                try
                {
                    object? convertedValue = prop.PropertyType == typeof(int)
                        ? int.Parse(item.Value ?? "0") // Provide a default value to handle null
                        : item.Value ?? ""; // Provide a default value for other types
                    prop.SetValue(settingsObj, convertedValue);
                }
                catch
                {
                    MessageBox.Show($"設定 [{item.Key}] の保存に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            SettingsManager.Save();
            MessageBox.Show("設定を保存しました！", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
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
