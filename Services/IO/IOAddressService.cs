using KdxDesigner.Models;
using KdxDesigner.Services.Access;
using KdxDesigner.Services.Error;
using KdxDesigner.Utils;

using System.Text;

namespace KdxDesigner.Services
{
    public class IOAddressService : IIOAddressService
    {
        private const string LengthPrefix = "L-";
        private const string UnderscorePrefix = "_";

        private readonly IErrorAggregator _errorAggregator;
        private readonly IAccessRepository _repository;
        private readonly int _plcId;

        public IOAddressService(IErrorAggregator errorAggregator, IAccessRepository repository, int plcId)
        {
            _errorAggregator = errorAggregator;
            _repository = repository;
            _plcId = plcId;
        }

        public string? GetSingleAddress(
            List<IO> ioList,
            string ioText,
            bool isOutput,
            string processName,
            int recordId)
        {
            ioList = isOutput
                ? ioList.Where(io => io.Address != null && io.Address.Contains("Y") || io.Address.Contains("Ｙ")).ToList()
                : ioList.Where(io => io.Address != null && (io.Address.Contains("X") || io.Address.Contains("Ｘ"))).ToList();
            // 内部で共通の検索ロジックを呼ぶ
            var result = FindByIOTextInternal(ioList, ioText, processName, recordId);

            switch (result.State)
            {
                case FindIOResultState.FoundOne:


                    return result.SingleAddress; // 成功ケース: アドレスを返す

                case FindIOResultState.FoundMultiple:
                    // ★ 複数件ヒットはエラーとして処理
                    _errorAggregator.AddError(new OutputError
                    {
                        Message = $"センサー '{ioText}' で複数の候補が見つかりました。一意に特定できません。",
                        RecordName = processName,
                        RecordId = recordId,
                    });
                    return null;

                case FindIOResultState.NotFound:
                default:
                    // ★ 0件ヒットはエラー (既に内部でエラー追加済み)
                    return null;
            }
        }

        public List<IO> GetAddressRange(
            List<IO> ioList,
            string ioText,
            string processName,
            int recordId,
            bool errorIfNotFound = false)
        {
            if (string.IsNullOrEmpty(ioText))
            {
                _errorAggregator.AddError(new OutputError
                {
                    Message = "IOテキストが指定されていません。",
                    RecordName = processName,
                    RecordId = recordId,
                });
                return new List<IO>();
            }

            string searchText = ioText.StartsWith("_") ? ioText.Substring(1) : ioText;
            var matches = ioList.Where(io => io.IOName != null && io.IOName.Contains(searchText)).ToList();

            if (!matches.Any() && errorIfNotFound)
            {
                _errorAggregator.AddError(new OutputError
                {
                    Message = $"指定された範囲のIO '{ioText}' が見つかりませんでした。",
                    RecordName = processName,
                    RecordId = recordId
                });
            }

            return matches;
        }
        private FindIOResult FindByIOTextInternal(
            List<IO> ioList,
            string ioText,
            string recordName,
            int? recordId)
        {
            if (string.IsNullOrEmpty(ioText))
            {
                _errorAggregator.AddError(new OutputError { Message = "IOテキストが指定されていません。", RecordName = recordName, RecordId = recordId });
                return new FindIOResult { State = FindIOResultState.NotFound };
            }

            // ... ("L-" プレフィックスの処理は変更なし) ...

            string searchText = ioText.StartsWith("_") ? ioText.Substring(1) : ioText;
            var matches = ioList.Where(io => io.IOName != null && io.IOName.Contains(searchText)).ToList();

            if (matches.Count == 0)
            {
                _errorAggregator.AddError(new OutputError { Message = $"センサー '{ioText}' が見つかりませんでした。", RecordName = recordName, RecordId = recordId });
                return new FindIOResult { State = FindIOResultState.NotFound };
            }

            if (matches.Count == 1)
            {
                var foundIo = matches[0];

                // ★★★ 修正箇所 ★★★
                // 1. LinkDevice に値があれば、それを優先する
                string? addressToReturn = !string.IsNullOrWhiteSpace(foundIo.LinkDevice)
                    ? foundIo.LinkDevice
                    : foundIo.Address;

                // 2. 返す前に全角を半角に正規化する
                string? normalizedAddress = addressToReturn?.Normalize(NormalizationForm.FormKC);

                return new FindIOResult
                {
                    State = FindIOResultState.FoundOne,
                    SingleAddress = normalizedAddress
                };
            }

            // 複数ヒットした場合、IOオブジェクトのリストをそのまま返す。
            // 呼び出し元が必要に応じて LinkDevice を参照できるようにするため。
            return new FindIOResult
            {
                State = FindIOResultState.FoundMultiple,
                MultipleMatches = matches
            };
        }
    }
}