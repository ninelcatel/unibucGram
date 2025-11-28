using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace unibucGram.Models
{
    public class Group
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string? Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDirectMessage { get; set; } = false;
        public string? ImageURL { get; set; } = string.Empty;

        public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
        public virtual ICollection<GroupMessage> Messages { get; set; } = new List<GroupMessage>();
    }
}