using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace unibucGram.Models
{
    public class Conversation
    {
        [Key]
        public int Id { get; set; }

        // conversatiile sunt intre 2 useri
        public string UserAId { get; set; } = null!; // FK
        public virtual User UserA { get; set; } = null!; // navigation

        public string UserBId { get; set; } = null!; // FK
        public virtual User UserB { get; set; } = null!; // navigation

        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}