using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("Company")]
    public class Company
    {
        [Key]
        public int Id { get; set; }
        public string? CompanyName { get; set; }
        public string? CreatedAt { get; set; }
    }
    
}

