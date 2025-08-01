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
        
        public ProcessFlowNode(ProcessDetail detail, Point position)
        {
            _processDetail = detail;
            _position = position;
        }
        
        public int Id => ProcessDetail.Id;
        public string DisplayName => ProcessDetail.DetailName ?? $"工程 {ProcessDetail.Id}";
        public string StartSensor => ProcessDetail.StartSensor ?? "";
        public string StartIds => ProcessDetail.StartIds ?? "";
    }
}