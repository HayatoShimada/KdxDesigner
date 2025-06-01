using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Utils.MnemonicCommon;

namespace KdxDesigner.Utils
{
    internal class ErrorBuilder
    {
        public static List<LadderCsvRow> Operation(
            Models.Operation operation,
            List<Error> error,
            string label,
            int outNum,
            out List<OutputError> errors)
        {
            List<LadderCsvRow>? result = new();
            errors = new List<OutputError>();
            var eachError = error.FirstOrDefault(e => e.RecordId == operation.Id);

            int countSpeed = 0;

            foreach (var each in error)
            {
                switch (each.AlarmId)
                    {
                    case 1: // 開始
                        AddError(errors, "エラーが発生しました。", each.Device ?? "", each.MnemonicId ?? 0, operation.Id);
                        result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                        result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 7).ToString()));
                        break;

                    case 2: // 開始確認
                        AddError(errors, "アラームが発生しました。", each.Device ?? "", each.MnemonicId ?? 0, operation.Id);
                        result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                        result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 7).ToString()));
                        result.Add(LadderRow.AddAND(label + (outNum + 18).ToString()));
                        break;

                    case 3: // 途中TO
                        AddError(errors, "注意が必要です。", each.Device ?? "", each.MnemonicId ?? 0, operation.Id);
                        result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                        result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 10 + countSpeed).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 18).ToString()));
                        countSpeed++;
                        break;

                    case 4: // 取り込みTO
                        AddError(errors, "注意が必要です。", each.Device ?? "", each.MnemonicId ?? 0, operation.Id);
                        result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                        result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 10 + countSpeed).ToString()));
                        result.Add(LadderRow.AddAND(label + (outNum + 18).ToString()));
                        break;

                    case 5: // 完了TO
                        AddError(errors, "注意が必要です。", each.Device ?? "", each.MnemonicId ?? 0, operation.Id);
                        result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
                        result.Add(LadderRow.AddAND(label + (outNum + 6).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 18).ToString()));
                        break;
                    default:
                        AddError(errors, "不明なエラータイプです。", each.Device ?? "", each.MnemonicId ?? 0, operation.Id);
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
                    AddError(
                        errors, $"エラーのデバイスが null または空です: '{operation.OperationName}'",
                        operation.OperationName ?? "", 3, 0);
                }

                // エラー番号のMOVセット
                result.AddRange(LadderRow.AddMOVSet("K" + each.ErrorNum.ToString(), SettingsManager.Settings.ErrorDevice));
            }
            
            return result;
        }

        private static void AddError(
            List<OutputError> errors,
            string message,
            string detailName,
            int mnemonicId,
            int processId)
        {
            errors.Add(new OutputError
            {
                Message = message,
                DetailName = detailName,
                MnemonicId = mnemonicId,
                ProcessId = processId
            });
        }
    }
}
