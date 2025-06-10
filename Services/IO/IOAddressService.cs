using KdxDesigner.Models;
using KdxDesigner.Models.Define; // OutputError, MnemonicType のため
using KdxDesigner.Services.Access;
using KdxDesigner.Services.Error;
using KdxDesigner.Utils;

using System.Collections.Generic;
using System.Linq;

namespace KdxDesigner.Services
{
    public class IOAddressService : IIOAddressService
    {
        private const string LengthPrefix = "L-";
        private const string UnderscorePrefix = "_";
        private const int DefaultErrorMnemonicId = 6;

        private readonly IErrorAggregator _errorAggregator;
        private readonly IAccessRepository _repository;

        public IOAddressService(IErrorAggregator errorAggregator, IAccessRepository repository)
        {
            _errorAggregator = errorAggregator;
            _repository = repository;
        }

        public FindIOResult FindByIOText(List<IO> ioList, string ioText, int plcId)
        {
            if (string.IsNullOrEmpty(ioText))
            {
                _errorAggregator.AddError(new OutputError { Message = "IOテキストが指定されていません。", DetailName = ioText });
                return new FindIOResult { State = FindIOResultState.NotFound };
            }

            // "L-" プレフィックスの処理
            if (ioText.StartsWith(LengthPrefix))
            {
                string lengthName = ioText.Substring(LengthPrefix.Length);
                if (string.IsNullOrEmpty(lengthName))
                {
                    _errorAggregator.AddError(new OutputError { Message = $"不正なL-プレフィックス形式です: '{ioText}'", DetailName = ioText });
                    return new FindIOResult { State = FindIOResultState.NotFound, SingleAddress = SettingsManager.Settings.OutErrorDevice };
                }

                List<Length>? lengths = _repository.GetLengthByPlcId(plcId);
                Length? length = lengths?.FirstOrDefault(l => l.LengthName == lengthName);

                if (length == null || string.IsNullOrEmpty(length.Device))
                {
                    _errorAggregator.AddError(new OutputError { Message = $"Lengthセンサー '{ioText}' が見つかりませんでした。", DetailName = ioText });
                    return new FindIOResult { State = FindIOResultState.NotFound, SingleAddress = SettingsManager.Settings.OutErrorDevice };
                }

                return new FindIOResult { State = FindIOResultState.FoundOne, SingleAddress = length.Device };
            }

            // 通常の検索処理
            string searchText = ioText.StartsWith(UnderscorePrefix)
                                ? ioText.Substring(UnderscorePrefix.Length)
                                : ioText;

            var matches = ioList.Where(io => io.IOName != null && io.IOName.Contains(searchText)).ToList();

            if (matches.Count == 0)
            {
                _errorAggregator.AddError(new OutputError { Message = $"センサー '{ioText}' が見つかりませんでした。", DetailName = ioText });
                return new FindIOResult { State = FindIOResultState.NotFound };
            }

            if (matches.Count == 1)
            {
                return new FindIOResult { State = FindIOResultState.FoundOne, SingleAddress = matches[0].Address };
            }

            // ★ UI表示ロジックを削除。代わりに複数候補を返す。
            // 呼び出し元 (ViewModel) がダイアログ表示の責務を持つ。
            return new FindIOResult { State = FindIOResultState.FoundMultiple, MultipleMatches = matches };
        }

        public List<IO>? FindByIORange(List<IO> ioList, string ioText)
        {
            if (string.IsNullOrEmpty(ioText))
            {
                _errorAggregator.AddError(new OutputError { Message = "IOテキストが指定されていません。", DetailName = ioText });
                return null;
            }
            if (ioText.StartsWith(LengthPrefix))
            {
                _errorAggregator.AddError(new OutputError { Message = "L-から始まるものは検索できません", DetailName = ioText });
                return null;
            }

            string searchText = ioText.StartsWith(UnderscorePrefix)
                                ? ioText.Substring(UnderscorePrefix.Length)
                                : ioText;

            var matches = ioList.Where(io => io.IOName != null && io.IOName.Contains(searchText)).ToList();

            // このメソッドでは見つからなくてもエラーとはしない（元のロジックを維持）
            return matches;
        }
    }
}