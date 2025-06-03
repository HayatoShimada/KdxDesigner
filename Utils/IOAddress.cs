using KdxDesigner.Models;
using KdxDesigner.Services;
using KdxDesigner.Views;
using System.Collections.Generic;
using System.Linq;

namespace KdxDesigner.Utils
{
    internal class IOAddress
    {
        private const string LengthPrefix = "L-";
        private const string UnderscorePrefix = "_";
        private const int DefaultErrorMnemonicId = 6; // 元のコードのエラーで使用されていたID

        public static string? FindByIOText(List<IO> ioList, string ioText, int plcId, out List<OutputError> errors)
        {
            errors = new List<OutputError>();

            if (string.IsNullOrEmpty(ioText))
            {
                AddError(errors, "IOテキストが指定されていません。", ioText, DefaultErrorMnemonicId, 0);
                return null;
            }

            if (ioText.StartsWith(LengthPrefix))
            {
                var lengthResult = SettingsManager.Settings.OutErrorDevice;
                AccessRepository repository = new AccessRepository(); // 元の設計に従いメソッド内でインスタンス化
                string lengthName = ioText.Substring(LengthPrefix.Length);

                if (string.IsNullOrEmpty(lengthName))
                {
                    AddError(errors, $"不正なL-プレフィックス形式です: '{ioText}'", ioText, DefaultErrorMnemonicId, 0);
                    return lengthResult;
                }

                List<Length>? lengths = repository.GetLengthByPlcId(plcId);
                Length? length = lengths?.FirstOrDefault(l => l.LengthName == lengthName);

                if (length == null || string.IsNullOrEmpty(length.Device))
                {
                    AddError(errors, $"Lengthセンサー '{ioText}' が見つかりませんでした。", ioText, DefaultErrorMnemonicId, 0);
                    return lengthResult;
                }

                return length.Device;
            }

            string searchText = ioText.StartsWith(UnderscorePrefix)
                              ? ioText.Substring(UnderscorePrefix.Length)
                              : ioText;

            var matches = ioList
                .Where(io => io.IOText != null && io.IOName.Contains(searchText))
                .ToList();

            if (matches.Count == 0)
            {
                AddError(errors, $"センサー '{ioText}' が見つかりませんでした。", ioText, DefaultErrorMnemonicId, 0);
                return null;
            }

            if (matches.Count == 1)
            {
                return matches[0].Address;
            }

            // 複数ヒットした場合
            var selector = new IOSelectView(matches);
            if (selector.ShowDialog() == true && !string.IsNullOrEmpty(selector.SelectedAddress))
            {
                return selector.SelectedAddress;
            }
            else
            {
                AddError(errors, $"センサー '{ioText}' の選択がキャンセルされたか、無効なアドレスが選択されました。", ioText, DefaultErrorMnemonicId, 0);
                return null;
            }
        }

        private static void AddError(List<OutputError> errors, string message, string originalIoText, int mnemonicId, int processId)
        {
            errors.Add(new OutputError
            {
                Message = message,
                DetailName = "", // 元のコードでは空文字列でした
                MnemonicId = mnemonicId,
                ProcessId = processId
            });
        }
    }
}
