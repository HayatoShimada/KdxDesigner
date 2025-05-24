using System.Collections.Generic;
using System.Linq;

using KdxDesigner.Utils;

namespace KdxDesigner.Models.Define
{
    public static class ProcessDetailExtensions
    {
        public static List<string> GetStartIdList(this ProcessDetailDto detail)
        {
            return ConvertIdsToAddressList(detail.StartIds);
        }

        public static List<string> GetFinishIdList(this ProcessDetailDto detail)
        {
            return ConvertIdsToAddressList(detail.FinishIds);
        }

        public static string ConvertIdToAddress(int id)
        {
            int offset = SettingsManager.Settings.AddressOffset;
            int value = id * 10 + offset;
            return $"L{value}";
        }

        public static List<string> ConvertIdToAddressRange(int id)
        {
            int offset = SettingsManager.Settings.AddressOffset;
            int baseValue = id * 10 + offset;

            return Enumerable.Range(0, 10)
                .Select(i => $"L{baseValue + i}")
                .ToList();
        }


        private static List<string> ConvertIdsToAddressList(string? ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return new List<string>();

            return ids
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id =>
                {
                    if (int.TryParse(id.Trim(), out var n))
                    {
                        return ConvertIdToAddress(n);
                    }
                    return null;
                })
                .Where(address => address != null)
                .ToList()!;
        }
    }
}
