using CommunityToolkit.Mvvm.ComponentModel;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KdxDesigner.Models
{
    [Table("ProcessCategory")]
    public class ProcessCategory : ObservableObject
    {
        [Key]
        public int Id { get; set; }
        public string? ProcessCategoryName { get; set; }

    }
}
