using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KdxDesigner.Models;
using KdxDesigner.Services;

using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace KdxDesigner.ViewModels
{
    public partial class MemoryProfileViewModel : ObservableObject
    {
        private readonly MemoryProfileManager _profileManager;
        private readonly MainViewModel _mainViewModel;

        [ObservableProperty] private ObservableCollection<MemoryProfile> profiles = new();
        [ObservableProperty] private MemoryProfile? selectedProfile;
        [ObservableProperty] private string newProfileName = string.Empty;
        [ObservableProperty] private string newProfileDescription = string.Empty;

        public MemoryProfileViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _profileManager = new MemoryProfileManager();
            LoadProfiles();
        }

        private void LoadProfiles()
        {
            var profileList = _profileManager.LoadProfiles();
            Profiles = new ObservableCollection<MemoryProfile>(profileList);
        }

        [RelayCommand]
        private void LoadProfile()
        {
            if (SelectedProfile == null)
            {
                MessageBox.Show("プロファイルを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"プロファイル「{SelectedProfile.Name}」を読み込みますか？\n現在の設定は上書きされます。",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _profileManager.ApplyProfileToViewModel(SelectedProfile, _mainViewModel);
                _mainViewModel.SaveLastUsedProfile(SelectedProfile.Id);
                MessageBox.Show("プロファイルを読み込みました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void SaveCurrentAsProfile()
        {
            if (string.IsNullOrWhiteSpace(NewProfileName))
            {
                MessageBox.Show("プロファイル名を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 同名のプロファイルが存在するかチェック
            if (Profiles.Any(p => p.Name == NewProfileName && !p.IsDefault))
            {
                var result = MessageBox.Show(
                    $"同名のプロファイル「{NewProfileName}」が既に存在します。上書きしますか？",
                    "確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            var newProfile = _profileManager.CreateProfileFromCurrent(_mainViewModel, NewProfileName, NewProfileDescription);
            _profileManager.SaveProfile(newProfile);
            
            LoadProfiles();
            NewProfileName = string.Empty;
            NewProfileDescription = string.Empty;
            
            MessageBox.Show("プロファイルを保存しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void UpdateProfile()
        {
            if (SelectedProfile == null)
            {
                MessageBox.Show("更新するプロファイルを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SelectedProfile.IsDefault)
            {
                MessageBox.Show("デフォルトプロファイルは更新できません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"プロファイル「{SelectedProfile.Name}」を現在の設定で更新しますか？",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var updatedProfile = _profileManager.CreateProfileFromCurrent(_mainViewModel, SelectedProfile.Name, SelectedProfile.Description);
                updatedProfile.Id = SelectedProfile.Id;
                updatedProfile.CreatedAt = SelectedProfile.CreatedAt;
                
                _profileManager.SaveProfile(updatedProfile);
                LoadProfiles();
                
                MessageBox.Show("プロファイルを更新しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void DeleteProfile()
        {
            if (SelectedProfile == null)
            {
                MessageBox.Show("削除するプロファイルを選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SelectedProfile.IsDefault)
            {
                MessageBox.Show("デフォルトプロファイルは削除できません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"プロファイル「{SelectedProfile.Name}」を削除しますか？",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _profileManager.DeleteProfile(SelectedProfile.Id);
                LoadProfiles();
                MessageBox.Show("プロファイルを削除しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        partial void OnSelectedProfileChanged(MemoryProfile? value)
        {
            if (value != null)
            {
                // 選択されたプロファイルの詳細を表示するなど
            }
        }
    }
}