using System;
using System.ComponentModel.DataAnnotations;

namespace T2207A_API.Models.Category
{
    public class CreateCategory
    {
        [Required]
        [MinLength(3, ErrorMessage = "Nhap toi thieu 3 ki tu")]
        [MaxLength(255, ErrorMessage = "Nhap toi thieu 255 ki tu")]
        public string name { get; set; }
    }
}
