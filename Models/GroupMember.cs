using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace unibucGram.Models
{
    public class GroupMember
    {
        
        public int Id { get; set; }
        
        public int GroupId { get; set; }
        
        public virtual Group? Group { get; set; }

        [Required]
        public string? UserId { get; set; } = string.Empty;
        [Required]
        public virtual User? User { get; set; } 
    }
}