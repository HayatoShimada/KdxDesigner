using KdxDesigner.Models;
using KdxDesigner.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KdxDesigner.Views
{
    /// <summary>
    /// MemoryEdit.xaml の相互作用ロジック
    /// </summary>
    public partial class MemoryEditorView : Window
    {
        public MemoryEditorView(int plcId)
        {
            InitializeComponent();
            DataContext = new MemoryEditorViewModel(plcId);
        }

        private void MemoryGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (sender is DataGrid grid && grid.CurrentCell != null)
                {
                    var clipboardText = Clipboard.GetText();
                    // → ここで `clipboardText` を分割して行・列に分けて grid に反映
                }
            }
        }


    }


}
