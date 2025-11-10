using System;
using System.ComponentModel.DataAnnotations;

namespace unibucGram.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        public int ConversationId { get; set; } // carui conversatie ii apartine
        public virtual Conversation Conversation { get; set; } = null!; // navigation

        public string SenderId { get; set; } = null!; // FK
        public virtual User Sender { get; set; } = null!; // navigation

        [StringLength(1000)]
        public string? Text { get; set; }
        public string? AttachmentUrl { get; set; }
        /*
        aceeasi problema ca la post:
        un mesaj poate sa nu aibe text sau attachment,
        dar trebuie sa aibe cel putin una din ele,
        IDEE: poate se va verifica la nivel de formular?
        */
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false; // seen status
    }
}