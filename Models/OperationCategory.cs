﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KdxDesigner.Models
{
    [Table("OperationCategory")]
    public class OperationCategory
    {
        [Key]
        public int Id { get; set; }
        public string? CategoryName { get; set; }
    }
}
