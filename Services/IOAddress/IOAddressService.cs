using KdxDesigner.Models;
using KdxDesigner.Services.Access;
using KdxDesigner.Services.Error;

using KdxDesigner.Utils;
using KdxDesigner.Services.IOSelectorService;
using System.Diagnostics;
using System.Text;

namespace KdxDesigner.Services.IOAddress
{
    public class IOAddressService : IIOAddressService
    {
        private const string LengthPrefix = "L-";
        private const string UnderscorePrefix = "_";

        private readonly IErrorAggregator _errorAggregator;
        private readonly IAccessRepository _repository;
        private readonly IIOSelectorService _ioSelectorService;
        private readonly int _plcId;

        public IOAddressService(
            IErrorAggregator errorAggregator,
            IAccessRepository repository,
            int plcId,
            IIOSelectorService ioSelectorService)
        {
            _errorAggregator = errorAggregator;
            _repository = repository;
            _plcId = plcId;
            _ioSelectorService = ioSelectorService; // ★ 依存性を保持
        }


        public string? GetSingleAddress(
            List<IO> ioList,
            string ioText,
            bool isOutput,
            string recordName,
            int? recordId,
            string? isnotInclude)
        {
            Debug.WriteLine($"GetSingleAddress called with:{isOutput} recordId={recordId}, processName='{recordName}', ioText='{ioText}'");
            Debug.WriteLine(ioList.Count);

            // isOutput (Y/X) でフィルタリング
            ioList = isOutput
                ? ioList.Where(io => io.Address != null && (io.Address.Contains("Y") || io.Address.Contains("Ｙ"))).ToList()
                : ioList.Where(io => io.Address != null && (io.Address.Contains("X") || io.Address.Contains("Ｘ"))).ToList();

            // isnotInclude パラメータに値が指定されている場合、その単語を含むIOを除外する
            // FindByIOTextInternal での検索対象である IOName プロパティに対してチェックを行う
            if (!string.IsNullOrEmpty(isnotInclude))
            {
                ioList = ioList.Where(io => io.IOName != null && !io.IOName.Contains(isnotInclude)).ToList();
            }

            var workingList = isOutput
                ? ioList.Where(io => io.Address != null && (io.Address.Contains("Y") || io.Address.Contains("Ｙ"))).ToList()
                : ioList.Where(io => io.Address != null && (io.Address.Contains("X") || io.Address.Contains("Ｘ"))).ToList();
            if (!string.IsNullOrEmpty(isnotInclude))
            {
                workingList = workingList.Where(io => io.IOName != null && !io.IOName.Contains(isnotInclude)).ToList();
            }

            var result = FindByIOTextInternal(workingList, ioText, recordName, recordId);

            switch (result.State)
            {
                case FindIOResultState.FoundOne:
                    return result.SingleAddress;

                case FindIOResultState.FoundMultiple:
                    // ★★★ 修正箇所 ★★★
                    // 複数件ヒットした場合、UI選択サービスを呼び出す
                    IO? selectedIo = _ioSelectorService.SelectIoFromMultiple(ioText, result.MultipleMatches!, recordName, recordId);

                    if (selectedIo != null)
                    {
                        // ユーザーが項目を選択した場合、そのアドレスを返す
                        // LinkDeviceが設定されていればそれを優先
                        string? addressToReturn = !string.IsNullOrWhiteSpace(selectedIo.LinkDevice)
                            ? selectedIo.LinkDevice
                            : selectedIo.Address;
                        return addressToReturn?.Normalize(NormalizationForm.FormKC);
                    }
                    else
                    {
                        // ユーザーが選択をキャンセルした場合、エラーとして報告
                        _errorAggregator.AddError(new OutputError
                        {
                            Message = $"IO '{ioText}' の選択がキャンセルされました。",
                            RecordName = recordName,
                            RecordId = recordId,
                            IsCritical = true // 処理を続行できないため致命的なエラーとする
                        });
                        return null;
                    }

                case FindIOResultState.NotFound:
                default:
                    // 0件ヒットはエラー (既に内部でエラー追加済み)
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

            if (ioText.StartsWith(LengthPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var lengthList = _repository.GetLengthByPlcId(_plcId);

                // ★★★ 修正箇所 スタート ★★★

                // Step 1: まず、条件に一致するオブジェクト自体を検索して変数に格納します。
                //         リストや各要素、プロパティがnullの可能性も考慮し、安全に検索します。
                var foundLengthObject = lengthList?.FirstOrDefault(s => s != null && s.LengthName != null && s.LengthName.Contains(ioText));

                // Step 2: オブジェクトが見つかった（nullではない）ことを確認します。
                if (foundLengthObject != null)
                {
                    // Step 3: オブジェクトがnullでないことを確認してから、安全にプロパティを取得します。
                    string? foundAddress = foundLengthObject.Device;

                    return new FindIOResult
                    {
                        State = FindIOResultState.FoundOne,
                        SingleAddress = foundAddress // 見つかったアドレスを返す
                    };
                }
                // オブジェクトが見つからなかった場合
                else
                {
                    // 見つからなかった場合はエラーとして処理し、NotFound を返す
                    _errorAggregator.AddError(new OutputError { Message = $"指定されたアドレス '{ioText}' (検索値: '{ioText}') が見つかりませんでした。", RecordName = recordName, RecordId = recordId });
                    return new FindIOResult { State = FindIOResultState.NotFound };
                }
                // ★★★ 修正箇所 エンド ★★★
            }

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

                string? addressToReturn = !string.IsNullOrWhiteSpace(foundIo.LinkDevice)
                    ? foundIo.LinkDevice
                    : foundIo.Address;

                string? normalizedAddress = addressToReturn?.Normalize(NormalizationForm.FormKC);

                return new FindIOResult
                {
                    State = FindIOResultState.FoundOne,
                    SingleAddress = normalizedAddress
                };
            }

            return new FindIOResult
            {
                State = FindIOResultState.FoundMultiple,
                MultipleMatches = matches
            };
        }
    }
}