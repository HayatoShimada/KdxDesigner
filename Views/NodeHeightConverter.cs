using System;
using System.Globalization;
using System.Windows.Data;

namespace KdxDesigner.Views
{
    public class NodeHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is bool showId && values[1] is bool showBlockNumber)
            {
                // ProcessIDも表示されるため、より大きな高さが必要
                if (showId && showBlockNumber)
                    return 70.0; // ID、ProcessID、BlockNumber すべて表示
                else if (showId || showBlockNumber)
                    return 65.0; // どちらか一方を表示（ProcessIDは常に表示される可能性）
                else
                    return 60.0; // ID・BlockNumber非表示（ProcessIDは表示される可能性）
            }
            return 60.0; // デフォルト（ProcessID表示を考慮）
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}