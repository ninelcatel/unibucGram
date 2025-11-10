using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace unibucGram.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Description { get; set; } = string.Empty; // e optionala descrierea

        

    } 
}