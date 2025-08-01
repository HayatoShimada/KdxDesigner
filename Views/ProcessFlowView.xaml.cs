using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using KdxDesigner.ViewModels;
using System.ComponentModel;

namespace KdxDesigner.Views
{
    /// <summary>
    /// ProcessFlowView.xaml の相互作用ロジック
    /// </summary>
    public partial class ProcessFlowView : Window
    {
        private ProcessFlowViewModel _viewModel;
        private Canvas? _mainCanvas;
        private ProcessDetailPropertiesWindow? _propertiesWindow;
        private ConnectionInfoWindow? _connectionInfoWindow;
        private ScrollViewer? _scrollViewer;
        private bool _isPanning = false;
        private Point _lastPanPoint;
        private Point _panStartScrollOffset;
        
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
            
            // ScrollViewerを取得
            _scrollViewer = GetScrollViewer();
            if (_scrollViewer != null)
            {
                _scrollViewer.PreviewMouseDown += OnScrollViewerMouseDown;
                _scrollViewer.PreviewMouseMove += OnScrollViewerMouseMove;
                _scrollViewer.PreviewMouseUp += OnScrollViewerMouseUp;
            }
            
            // プロパティウィンドウを開く
            ShowPropertiesWindow();
            
            // ViewModelのSelectedNodeプロパティの変更を監視
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            
            // 接続選択イベントを監視
            _viewModel.ConnectionSelected += OnConnectionSelected;
        }
        
        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            _viewModel.CanvasMouseMoveCommand.Execute(e);
        }
        
        private ScrollViewer? GetScrollViewer()
        {
            // ビジュアルツリーからScrollViewerを検索
            var grid = Content as Grid;
            if (grid != null && grid.Children.Count > 1)
            {
                return grid.Children[1] as ScrollViewer;
            }
            return null;
        }
        
        private void OnScrollViewerMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed && _scrollViewer != null)
            {
                _isPanning = true;
                _lastPanPoint = e.GetPosition(_scrollViewer);
                _panStartScrollOffset = new Point(_scrollViewer.HorizontalOffset, _scrollViewer.VerticalOffset);
                _scrollViewer.Cursor = Cursors.ScrollAll;
                _scrollViewer.CaptureMouse();
                e.Handled = true;
            }
        }
        
        private void OnScrollViewerMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning && e.MiddleButton == MouseButtonState.Pressed && _scrollViewer != null)
            {
                var currentPoint = e.GetPosition(_scrollViewer);
                var deltaX = currentPoint.X - _lastPanPoint.X;
                var deltaY = currentPoint.Y - _lastPanPoint.Y;
                
                _scrollViewer.ScrollToHorizontalOffset(_panStartScrollOffset.X - deltaX);
                _scrollViewer.ScrollToVerticalOffset(_panStartScrollOffset.Y - deltaY);
                
                e.Handled = true;
            }
        }
        
        private void OnScrollViewerMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released && _isPanning && _scrollViewer != null)
            {
                _isPanning = false;
                _scrollViewer.Cursor = Cursors.Arrow;
                _scrollViewer.ReleaseMouseCapture();
                e.Handled = true;
            }
        }
        
        private void ShowPropertiesWindow()
        {
            if (_propertiesWindow == null || !_propertiesWindow.IsLoaded)
            {
                _propertiesWindow = new ProcessDetailPropertiesWindow
                {
                    DataContext = _viewModel,
                    Owner = this
                };
                
                // ウィンドウの位置を設定（メインウィンドウの右側）
                _propertiesWindow.Left = this.Left + this.Width + 10;
                _propertiesWindow.Top = this.Top;
                
                // ウィンドウが閉じられたときの処理
                _propertiesWindow.Closed += (s, e) => _propertiesWindow = null;
                
                _propertiesWindow.Show();
            }
        }
        
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // ノードが選択されたときにプロパティウィンドウを表示
            if (e.PropertyName == nameof(ProcessFlowViewModel.SelectedNode) && _viewModel.SelectedNode != null)
            {
                if (_propertiesWindow == null || !_propertiesWindow.IsLoaded)
                {
                    ShowPropertiesWindow();
                }
                else
                {
                    // ウィンドウが既に開いている場合はアクティブにする
                    _propertiesWindow.Activate();
                }
            }
        }
        
        private void OnConnectionSelected(object? sender, EventArgs e)
        {
            if (_viewModel.SelectedConnection != null)
            {
                // 既存の接続情報ウィンドウがあれば閉じる
                _connectionInfoWindow?.Close();
                
                // 新しい接続情報ウィンドウを作成して表示
                _connectionInfoWindow = new ConnectionInfoWindow
                {
                    DataContext = _viewModel,
                    Owner = this
                };
                
                _connectionInfoWindow.Closed += (s, args) => _connectionInfoWindow = null;
                _connectionInfoWindow.ShowDialog();
            }
        }
        
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // プロパティウィンドウも閉じる
            _propertiesWindow?.Close();
            
            // 接続情報ウィンドウも閉じる
            _connectionInfoWindow?.Close();
            
            // イベントハンドラーの解除
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.ConnectionSelected -= OnConnectionSelected;
            }
            
            if (_scrollViewer != null)
            {
                _scrollViewer.PreviewMouseDown -= OnScrollViewerMouseDown;
                _scrollViewer.PreviewMouseMove -= OnScrollViewerMouseMove;
                _scrollViewer.PreviewMouseUp -= OnScrollViewerMouseUp;
            }
        }
    }
}