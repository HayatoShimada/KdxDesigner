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

        // ★ 新メソッド: 単一のアドレスを期待する場合
        public string? GetSingleAddress(
            List<IO> ioList, 
            string ioText, 
            int plcId, 
            bool isOutput,
            string processName,
            int mnemonicId,
            int recordId)
        {
            
            if (isOutput)
            {
                ioList = ioList.Where(io => io.Address.Contains("Y")).ToList();
            }
            else
            {
                ioList = ioList.Where(io => io.Address.Contains("X")).ToList();
            }
            // 内部で共通の検索ロジックを呼ぶ
            var result = FindByIOTextInternal(ioList, ioText, plcId, processName, mnemonicId, recordId);

            switch (result.State)
                {
                    case FindIOResultState.FoundOne:
                        return result.SingleAddress; // 成功ケース: アドレスを返す

                    case FindIOResultState.FoundMultiple:
                        // ★ 複数件ヒットはエラーとして処理
                        _errorAggregator.AddError(new OutputError { Message = $"センサー '{ioText}' で複数の候補が見つかりました。一意に特定できません。", 
                            RecordName = processName, 
                            MnemonicId = mnemonicId,
                            RecordId = recordId,
                        });
                        return null;

                    case FindIOResultState.NotFound:
                    default:
                        // ★ 0件ヒットはエラー (既に内部でエラー追加済み)
                        return null;
                }
        }

        // ★ 新メソッド: 複数(0件以上)のIOを期待する場合
        public List<IO> GetAddressRange(
            List<IO> ioList, 
            string ioText, 
            string processName, 
            int mnemonicId, 
            int recordId,
            bool errorIfNotFound = false)
        {
            if (string.IsNullOrEmpty(ioText))
            {
                _errorAggregator.AddError(new OutputError { 
                    Message = "IOテキストが指定されていません。",
                    RecordName = processName,
                    MnemonicId = mnemonicId,
                    RecordId = recordId,
                });
                return new List<IO>();
            }

            string searchText = ioText.StartsWith("_") ? ioText.Substring(1) : ioText;
            var matches = ioList.Where(io => io.IOName != null && io.IOName.Contains(searchText)).ToList();

            if (!matches.Any() && errorIfNotFound)
            {
                _errorAggregator.AddError(new OutputError { 
                    Message = $"指定された範囲のIO '{ioText}' が見つかりませんでした。", 
                    RecordName = processName,
                    MnemonicId = mnemonicId,
                    RecordId = recordId
                });
            }

            return matches;
        }


        private FindIOResult FindByIOTextInternal(
            List<IO> ioList, 
            string ioText, 
            int plcId,
            string processName,
            int mnemonicId,
            int recordId
            )
        {
            if (string.IsNullOrEmpty(ioText))
            {
                _errorAggregator.AddError(new OutputError { 
                    Message = "IOテキストが指定されていません。", 
                    RecordName = processName,
                    MnemonicId = mnemonicId,
                    RecordId = recordId
                });
                return new FindIOResult { State = FindIOResultState.NotFound };
            }

            // "L-" プレフィックスの処理
            if (ioText.StartsWith(LengthPrefix))
            {
                string lengthName = ioText.Substring(LengthPrefix.Length);
                if (string.IsNullOrEmpty(lengthName))
                {
                    _errorAggregator.AddError(new OutputError { 
                        Message = $"不正なL-プレフィックス形式です: '{ioText}'", 
                        RecordName = processName,
                        MnemonicId = mnemonicId,
                        RecordId = recordId
                    });
                    return new FindIOResult { State = FindIOResultState.NotFound, SingleAddress = SettingsManager.Settings.OutErrorDevice };
                }

                List<Length>? lengths = _repository.GetLengthByPlcId(plcId);
                Length? length = lengths?.FirstOrDefault(l => l.LengthName.Contains(lengthName));

                if (length == null || string.IsNullOrEmpty(length.Device))
                {
                    _errorAggregator.AddError(new OutputError { 
                        Message = $"Lengthセンサー '{ioText}' が見つかりませんでした。", 
                        RecordName = processName, MnemonicId = mnemonicId, RecordId = recordId });
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
                _errorAggregator.AddError(new OutputError { 
                    Message = $"センサー '{ioText}' が見つかりませんでした。", 
                    RecordName = processName,
                    MnemonicId = mnemonicId,
                    RecordId = recordId
                });
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
    }
}