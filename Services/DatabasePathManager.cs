using KdxDesigner.Utils.ini;

using Microsoft.Win32;

using System;
using System.IO;
using System.Windows;

namespace KdxDesigner.Services
{
    public class DatabasePathManager
    {
        private readonly string _iniPath;

        /// <summary>
        /// 現在設定されているデータベースのパスを保持します。
        /// </summary>
        public string? CurrentDatabasePath { get; private set; }

        public DatabasePathManager()
        {
            _iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
            // コンストラクタでiniファイルから現在のパスを読み込む
            CurrentDatabasePath = IniHelper.ReadValue("Database", "AccessPath", _iniPath);
        }

        /// <summary>
        /// アプリケーション起動時に、有効なデータベースパスを解決します。
        /// パスが無効な場合は、ユーザーに選択を促します。
        /// </summary>
        /// <returns>有効なデータベースパス。</returns>
        /// <exception cref="InvalidOperationException">ユーザーがパス選択をキャンセルした場合。</exception>
        public string ResolveDatabasePath()
        {
            while (string.IsNullOrWhiteSpace(CurrentDatabasePath) || !File.Exists(CurrentDatabasePath))
            {
                if (!string.IsNullOrWhiteSpace(CurrentDatabasePath))
                {
                    MessageBox.Show($"指定されたAccessファイルが見つかりませんでした。\nパス: {CurrentDatabasePath}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // SelectAndSaveNewPathを呼び出してパス選択を促す
                if (!SelectAndSaveNewPath())
                {
                    // キャンセルされた場合は例外をスローしてアプリを終了させる
                    throw new InvalidOperationException("Accessファイルの選択がキャンセルされたため、アプリケーションを続行できません。");
                }
            }
            return CurrentDatabasePath!;
        }

        /// <summary>
        /// ★【新規】ファイル選択ダイアログを表示し、ユーザーが選択したパスをiniファイルに保存します。
        /// </summary>
        /// <returns>パスが正常に選択・保存された場合はtrue、キャンセルされた場合はfalseを返します。</returns>
        public bool SelectAndSaveNewPath()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Access DBファイル (*.accdb)|*.accdb",
                Title = "Accessデータベースファイルを選択"
            };

            if (dialog.ShowDialog() == true)
            {
                CurrentDatabasePath = dialog.FileName;
                IniHelper.WriteValue("Database", "AccessPath", CurrentDatabasePath, _iniPath);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 取得したパスから接続文字列を生成します。
        /// </summary>
        public string CreateConnectionString(string dbPath)
        {
            return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";
        }
    }
}