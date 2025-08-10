using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace KdxDesigner.Models
{
    public partial class ProcessFlowNode : ObservableObject
    {
        [ObservableProperty] private ProcessDetail _processDetail;
        [ObservableProperty] private Point _position;
        [ObservableProperty] private bool _isSelected;
        [ObservableProperty] private bool _isDragging;
        [ObservableProperty] private double _opacity = 1.0;
        [ObservableProperty] private bool _isModified = false;
        
        public ProcessFlowNode(ProcessDetail detail, Point position)
        {
            _processDetail = detail;
            _position = position;
        }
        
        public int Id => ProcessDetail.Id;
        public string DisplayName => ProcessDetail.DetailName ?? $"工程 {ProcessDetail.Id}";
        public string StartSensor => ProcessDetail.StartSensor ?? "";
        public int? CategoryId => ProcessDetail.CategoryId;
        
        // Detailプロパティへのショートカット（ViewModelとの互換性のため）
        public ProcessDetail Detail => ProcessDetail;
        
        // カテゴリ名を表示するためのプロパティ
        [ObservableProperty] private string? _categoryName;
        
        // 複合工程名を表示するためのプロパティ（BlockNumberが複合工程IDの場合）
        [ObservableProperty] private string? _compositeProcessName;
        
        // 表示名を更新するメソッド
        public void UpdateDisplayName()
        {
            OnPropertyChanged(nameof(DisplayName));
        }
        
        // 開始センサーが設定されているかどうか
        public bool HasStartSensor => !string.IsNullOrEmpty(ProcessDetail.StartSensor);
        
        // 開始センサーは設定されているがタイマーが未設定かどうか
        public bool HasStartSensorWithoutTimer => HasStartSensor && ProcessDetail.StartTimerId == null;
        
        // プロパティ変更通知メソッド
        public void NotifyStartSensorPropertiesChanged()
        {
            OnPropertyChanged(nameof(HasStartSensor));
            OnPropertyChanged(nameof(HasStartSensorWithoutTimer));
        }
        
        // 他サイクルのノードかどうかを示すプロパティ
        [ObservableProperty] private bool _isOtherCycleNode = false;
        
        // ノードの現在の高さ（表示オプションに応じて変動）
        [ObservableProperty] private double _nodeHeight = 45;
        
        // ノードの幅（固定）- ProcessID表示のため幅を拡張
        public double NodeWidth => 180;
    }
}