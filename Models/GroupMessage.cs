using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace unibucGram.Models
{
    public class GroupMessage
    {
        
        public int Id { get; set; }

        public int GroupId { get; set; }
        
        public virtual Group Group { get; set; }

        [Required, StringLength(2000)]
        public string? Content { get; set; } = string.Empty;
        
        public string? UserId { get; set; } = string.Empty;

        public virtual User? User { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}