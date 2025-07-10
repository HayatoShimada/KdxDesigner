using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services.Error;
using KdxDesigner.Utils.MnemonicCommon;

namespace KdxDesigner.Utils.Operation
{
    internal class ErrorBuilder
    {
        private readonly IErrorAggregator _errorAggregator;

        public ErrorBuilder(IErrorAggregator errorAggregator)
        {
            _errorAggregator = errorAggregator;
        }

        public List<LadderCsvRow> Operation(
            Models.Operation operation,
            List<Error> error,
            string label,
            int outNum)
        {
            List<LadderCsvRow>? result = new();

            List<Error> errorList = error.Where(e => e.RecordId == operation.Id).ToList();

            int countSpeed = 0;

            foreach (var each in errorList)
            {
                switch (each.AlarmId)
                    {
                    case 1: // 開始
                        result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                        result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 7).ToString()));
                        break;

                    case 2: // 開始確認
                        result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                        result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 7).ToString()));
                        result.Add(LadderRow.AddAND(label + (outNum + 19).ToString()));
                        break;

                    case 3: // 途中TO
                        result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                        result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 10 + countSpeed).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 19).ToString()));
                        countSpeed++;
                        break;

                    case 4: // 取り込みTO
                        result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                        result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 10 + countSpeed).ToString()));
                        result.Add(LadderRow.AddAND(label + (outNum + 19).ToString()));
                        break;

                    case 5: // 完了TO
                        result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                        result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 19).ToString()));
                        break;

                    default:
                        AddError("不明なエラータイプです。", each.Device ?? "", each.MnemonicId ?? 0, operation.Id);
                        continue;

                }

                // アウトコイルの出力
                if (!string.IsNullOrEmpty(each.Device))
                {
                    result.Add(LadderRow.AddOUT(each.Device));
                }
                else
                {
                    result.Add(LadderRow.AddOUT(SettingsManager.Settings.OutErrorDevice));
                    AddError($"エラーのデバイスが null または空です: '{operation.OperationName}'",
                        operation.OperationName ?? "", 3, 0);
                }

                // エラー番号のMOVセット
                result.AddRange(LadderRow.AddMOVSet("K" + each.ErrorNum.ToString(), SettingsManager.Settings.ErrorDevice));
            }
            
            return result;
        }

        private void AddError(
            string message,
            string detailName,
            int mnemonicId,
            int processId)
        {
            _errorAggregator.AddError(new OutputError
            {
                Message = message,
                RecordName = detailName,
                MnemonicId = mnemonicId,
                RecordId = processId
            });
        }
    }
}
