﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("Cycle")]
    public class Cycle
    {
        [Key]
        public int Id { get; set; }
        public int PlcId { get; set; }
        public string? CycleName { get; set; }
    }
    
}

