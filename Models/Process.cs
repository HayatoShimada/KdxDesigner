using CommunityToolkit.Mvvm.ComponentModel;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KdxDesigner.Models
{
    [Table("Process")]
    public class Process : ObservableObject
    {
        [Key]
        public int Id { get; set; }
        public string? ProcessName { get; set; }
        public int? CycleId { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
