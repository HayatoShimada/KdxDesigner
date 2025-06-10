using KdxDesigner.Utils.ini;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace KdxDesigner.Services
{
    public class DatabasePathManager
    {
        private readonly string _iniPath;

        public DatabasePathManager()
        {
            _iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
        }

        // データベースへの有効なパスを取得またはユーザーに選択させる
        public string ResolveDatabasePath()
        {
            string? dbPath = IniHelper.ReadValue("Database", "AccessPath", _iniPath);

            // パスが設定されていない、またはファイルが存在しない場合はダイアログを表示
            while (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
            {
                if (!string.IsNullOrWhiteSpace(dbPath))
                {
                    MessageBox.Show($"指定されたAccessファイルが見つかりませんでした。\nパス: {dbPath}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                MessageBox.Show("Accessファイルのパスが設定されていません。ファイルを選択してください。", "通知", MessageBoxButton.OK, MessageBoxImage.Information);

                var dialog = new OpenFileDialog
                {
                    Filter = "Access DBファイル (*.accdb)|*.accdb",
                    Title = "Accessファイルを選択"
                };

                if (dialog.ShowDialog() == true)
                {
                    dbPath = dialog.FileName;
                    IniHelper.WriteValue("Database", "AccessPath", dbPath, _iniPath);
                }
                else
                {
                    // キャンセルされた場合はアプリケーションを終了するか、例外をスローする
                    throw new InvalidOperationException("Accessファイルの選択がキャンセルされたため、アプリケーションを続行できません。");
                }
            }

            return dbPath;
        }

        // 取得したパスから接続文字列を生成する
        public string CreateConnectionString(string dbPath)
        {
            return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";
        }
    }
}