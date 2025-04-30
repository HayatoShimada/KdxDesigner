using KdxDesigner.Views;
using KdxDesigner.Models;

using System.Collections.Generic;
using System.Linq;

namespace KdxDesigner.Utils
{
    internal class IOAddress
    {
        public static string? Find(List<IO> ioList, int id)
        {
            return ioList.FirstOrDefault(io => io.Id == id)?.Address;
        }

        public static string? FindByIOText(List<IO> ioList, string ioText)
        {
            var matches = ioList
                .Where(io => !string.IsNullOrEmpty(io.IOText) && io.IOText.Contains(ioText))
                .ToList();

            if (matches.Count == 0)
            {
                return null;
            }
            else if (matches.Count == 1)
            {
                return matches[0].Address;
            }
            else
            {
                // 複数ヒット → 選択画面を表示
                var selector = new IOSelectView(matches);
                if (selector.ShowDialog() == true)
                {
                    return selector.SelectedAddress;
                }
                else
                {
                    return null; // キャンセルされた場合
                }
            }
        }
    }
}
