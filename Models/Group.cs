using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace unibucGram.Models
{
    public class Group
    {
        
        public int Id { get; set; }

        [Required(ErrorMessage = "Group name is required")]
        [StringLength(100)]
        public string? Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
        public virtual ICollection<GroupMessage> Messages { get; set; } = new List<GroupMessage>();
    }
}