using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Utils.MnemonicCommon;

namespace KdxDesigner.Utils
{
    internal class ErrorBuilder
    {
        public static List<LadderCsvRow> Operation(
            Models.Operation operation,
            Error error,
            string ldText,
            string ldiTest,
            out List<OutputError> errors)
        {
            List<LadderCsvRow>? result = new();
            errors = new List<OutputError>();

            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(ldText));
            result.Add(LadderRow.AddANI(ldText));

            if (!string.IsNullOrEmpty(error.Device))
            {
                result.Add(LadderRow.AddOUT(error.Device));
            }
            else
            {
                result.Add(LadderRow.AddOUT(SettingsManager.Settings.OutErrorDevice));
                AddError(
                    errors, $"エラーのデバイスが null または空です: '{operation.OperationName}'",
                    operation.OperationName ?? "", 3, 0);
            }

            result.AddRange(LadderRow.AddMOVSet("K" + error.ErrorNum.ToString(), SettingsManager.Settings.ErrorDevice));
            return result;
        }

        private static void AddError(List<OutputError> errors, string message, string detailName, int mnemonicId, int processId)
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
