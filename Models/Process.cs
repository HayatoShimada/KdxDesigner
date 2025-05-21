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
        public string? TestStart {  get; set; }
        public string? TestCondition { get; set; }
        public string? Autocondition { get; set; }
        public string? AutoMode { get; set; }
        public string? ProcessCategory { get; set; }
        public int? FinishId { get; set; }
        public string? ILStart { get; set; }

    }
}
