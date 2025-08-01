using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using KdxDesigner.ViewModels;

namespace KdxDesigner.Views
{
    /// <summary>
    /// ProcessFlowView.xaml の相互作用ロジック
    /// </summary>
    public partial class ProcessFlowView : Window
    {
        private ProcessFlowViewModel _viewModel;
        private Canvas? _mainCanvas;
        
        public ProcessFlowView(ProcessFlowViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            // Loadedイベントでコントロールを取得
            Loaded += OnLoaded;
        }
        
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Canvasを名前で検索
            _mainCanvas = FindName("MainCanvas") as Canvas;
            if (_mainCanvas != null)
            {
                _mainCanvas.MouseMove += OnCanvasMouseMove;
            }
        }
        
        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            _viewModel.CanvasMouseMoveCommand.Execute(e);
        }
    }
}