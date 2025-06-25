using KdxDesigner.Models;
using KdxDesigner.Services.Access;
using KdxDesigner.Utils;
using KdxDesigner.ViewModels;

using System.Collections.Generic;
using System.Linq;

namespace KdxDesigner.Services
{
    public class LinkDeviceService
    {
        private readonly IAccessRepository _repository;
        // エラー集約サービスもコンストラクタで受け取ることを推奨
        // private readonly IErrorAggregator _errorAggregator;

        public LinkDeviceService(IAccessRepository repository)
        {
            _repository = repository;
        }

        public bool CreateLinkDeviceRecords(PLC mainPlc, List<PlcLinkSettingViewModel> selectedSettings)
        {
            var allIoData = _repository.GetIoList();
            var ioRecordsToUpdate = new List<IO>();

            foreach (var setting in selectedSettings)
            {
                var subordinateIoList = allIoData
                    .Where(io => io.PlcId == setting.Plc.Id && io.Address != null && !io.Address.StartsWith("F"))
                    .ToList();

                // Xデバイスの処理
                ProcessDeviceType(subordinateIoList, "X", setting.XDeviceStart, ioRecordsToUpdate);

                // Yデバイスの処理
                ProcessDeviceType(subordinateIoList, "Y", setting.YDeviceStart, ioRecordsToUpdate);
            }

            if (ioRecordsToUpdate.Any())
            {
                _repository.UpdateIoLinkDevices(ioRecordsToUpdate);
            }

            // TODO: Memoryテーブルへの転送ロジック

            return true;
        }

        /// <summary>
        /// 特定のデバイス種別（XまたはY）のリンク処理を実行します。
        /// </summary>
        private void ProcessDeviceType(List<IO> subordinateIoList, string devicePrefix, string? linkStartAddress, List<IO> masterUpdateList)
        {
            if (string.IsNullOrEmpty(linkStartAddress)) return;

            // 1. 対象のデバイスを抽出し、アドレスで並び替える（連番の基準にするため重要）
            var devicesToProcess = subordinateIoList
                .Where(io => io.Address!.StartsWith(devicePrefix))
                .OrderBy(io => io.Address)
                .ToList();

            if (!devicesToProcess.Any()) return;

            // 2. メインPLC側のリンク開始アドレスを数値に変換
            if (!LinkDeviceCalculator.TryParseLinkAddress(linkStartAddress, out string mainPrefix, out long mainStartOffsetValue))
            {
                // TODO: エラー処理（例: _errorAggregator.AddError(...)）
                return;
            }

            // 3. ソート済みリストの順番（インデックス）をオフセットとして利用し、連番を生成
            for (int i = 0; i < devicesToProcess.Count; i++)
            {
                var currentIoDevice = devicesToProcess[i];

                // オフセット = リスト内でのインデックス（順番）
                long relativeOffset = i;

                // 最終的なリンク先アドレスのオフセット値を計算
                long finalLinkOffsetValue = mainStartOffsetValue + relativeOffset;

                // 計算結果を "Wxxxx.F" のような文字列にフォーマット
                string calculatedLinkDevice = LinkDeviceCalculator.FormatLinkAddress(mainPrefix, finalLinkOffsetValue);

                // IOオブジェクトのLinkDeviceプロパティを更新し、更新リストに追加
                currentIoDevice.LinkDevice = calculatedLinkDevice;
                masterUpdateList.Add(currentIoDevice);
            }
        }
    }
}