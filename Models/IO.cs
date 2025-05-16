using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("IO")]
    public class IO
    {
        [Key]
        public int Id { get; set; }
        public string? IOText { get; set; }
        public string? XComment { get; set; }
        public string? YComment { get; set; }
        public string? FComment { get; set; }
        public string? Address { get; set; }
        public string? IOName { get; set; }
        public string? IOExplanation { get; set; }
        public string? IOSpot { get; set; }
        public string? UnitName { get; set; }
        public string? System { get; set; }
        public string? StationNumber { get; set; }
        public string? IONameNaked { get; set; }
        public int? PlcId { get; set; }
        public string? LinkDevice { get; set; }
    }
}
