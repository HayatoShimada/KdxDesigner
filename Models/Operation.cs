using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("Operation")]
    public class Operation
    {
        [Key]
        public int Id { get; set; }
        public string? OperationName { get; set; }
        public int? CYId { get; set; }
        public int? CategoryId { get; set; }
        public string? Stay { get; set; }
        public string? Start { get; set; }
        public string? Finish { get; set; }
        public string? Valve1 { get; set; }
        public string? S1 { get; set; }
        public string? S2 { get; set; }
        public string? S3 { get; set; }
        public string? S4 { get; set; }
        public string? S5 { get; set; }
        public string? SS1 { get; set; }
        public string? SS2 { get; set; }
        public string? SS3 { get; set; }
        public string? SS4 { get; set; }
        public int? PIL { get; set; }
        public int? SC { get; set; }
        public int? FC { get; set; }
    }
}

