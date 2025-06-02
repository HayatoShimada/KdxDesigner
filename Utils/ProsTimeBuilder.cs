using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Utils.MnemonicCommon;

namespace KdxDesigner.Utils
{
    internal class ProsTimeBuilder
    {
        public static List<LadderCsvRow> Retention(
           Models.Operation operation,
           List<ProsTime> prosTimes,
           string label,
           int outNum,
           out List<OutputError> errors)
        {
            List<LadderCsvRow>? result = new();
            errors = new List<OutputError>();
            List<ProsTime> prosTimeList = prosTimes.Where(p => p.RecordId == operation.Id).ToList();

            // カウント信号の追加
            result.Add(LadderRow.AddLD(SettingsManager.Settings.PauseSignal));
            result.Add(LadderRow.AddAND(label + (outNum + 5).ToString()));
            result.Add(LadderRow.AddANI(label + (outNum + 19).ToString()));
            result.Add(LadderRow.AddAND(SettingsManager.Settings.Clock01));
            result.Add(LadderRow.AddPLS(label + (outNum + 3).ToString()));

            string previousDevice0 = "";
            string previousDevice1 = "";
            string previousDevice2 = "";
            string previousDevice3 = "";
            string previousDevice4 = "";

            foreach (var pros in prosTimeList)
            {
                if (String.IsNullOrEmpty(pros.CurrentDevice) || String.IsNullOrEmpty(pros.PreviousDevice)) continue;

                switch (pros.SortId)
                {                     
                    case 0: // 工程
                        result.Add(LadderRow.AddLD(label + (outNum + 5).ToString()));
                        result.AddRange(LadderRow.AddMOVPSet(SettingsManager.Settings.CycleTime, pros.CurrentDevice));
                        previousDevice0 = pros.PreviousDevice;
                        break;
                    case 1: // 出力可
                        result.Add(LadderRow.AddLD(label + (outNum + 3).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 6).ToString()));
                        result.Add(LadderRow.AddINC(pros.CurrentDevice));
                        previousDevice1 = pros.PreviousDevice;
                        break;
                    case 2: // 開始
                        result.Add(LadderRow.AddLD(label + (outNum + 3).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 7).ToString()));
                        result.Add(LadderRow.AddINC(pros.CurrentDevice));
                        previousDevice2 = pros.PreviousDevice;
                        break;
                    case 3: // 終了位置
                        result.Add(LadderRow.AddLD(label + (outNum + 3).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 17).ToString()));
                        result.Add(LadderRow.AddINC(pros.CurrentDevice));
                        previousDevice3 = pros.PreviousDevice;
                        break;
                    case 4: // 完了
                        result.Add(LadderRow.AddLD(label + (outNum + 3).ToString()));
                        result.Add(LadderRow.AddANI(label + (outNum + 19).ToString()));
                        result.Add(LadderRow.AddINC(pros.CurrentDevice));
                        previousDevice4 = pros.PreviousDevice;
                        break;
                }
            }

            var current = prosTimeList[0].CurrentDevice;
            var previous = prosTimeList[0].PreviousDevice;
            var count = "K" + prosTimeList.Count.ToString();

            // リセット信号の追加
            if (String.IsNullOrEmpty(current) || String.IsNullOrEmpty(previous)) return result;
            result.Add(LadderRow.AddLDP(label + (outNum + 19).ToString()));
            result.AddRange(LadderRow.AddBMOVSet(current, previous, count));
            result.AddRange(LadderRow.AddFMOVSet("K0", current, count));

            // CYタイム 全体
            string cylinderDevice = prosTimeList.Where(p => p.SortId == 0).FirstOrDefault().CylinderDevice ?? "";
            result.Add(LadderRow.AddLDP(label + (outNum + 19).ToString()));
            result.AddRange(LadderRow.AddMOVPSet(
                previousDevice4,
                cylinderDevice));

            // CYタイム　出力可
            cylinderDevice = prosTimeList.Where(p => p.SortId == 1).FirstOrDefault().CylinderDevice ?? "";
            result.AddRange(LadderRow.AddSUBP(
                "K0",
                previousDevice1, 
                cylinderDevice));

            // CYタイム　開始
            cylinderDevice = prosTimeList.Where(p => p.SortId == 2).FirstOrDefault().CylinderDevice ?? "";
            result.AddRange(LadderRow.AddSUBP(
                previousDevice1,
                previousDevice2,
                cylinderDevice));

            // CYタイム　出力停止
            cylinderDevice = prosTimeList.Where(p => p.SortId == 3).FirstOrDefault().CylinderDevice ?? "";
            result.AddRange(LadderRow.AddSUBP(
                previousDevice2,
                previousDevice3,
                cylinderDevice));

            // CYタイム　完了
            cylinderDevice = prosTimeList.Where(p => p.SortId == 4).FirstOrDefault().CylinderDevice ?? "";
            result.AddRange(LadderRow.AddSUBP(
                previousDevice3,
                previousDevice4,
                cylinderDevice));

            return result;
        }
    }
}
