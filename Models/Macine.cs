using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    public class Machine
    {
        public int Id { get; set; }
        public string MacineName { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
    }
}
