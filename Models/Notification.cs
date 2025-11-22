using System;
using System.ComponentModel.DataAnnotations;

namespace unibucGram.Models
{
    public enum NotificationType
    {
        Like = 1,
        Comment = 2,
        Follow = 3,
        FollowRequest = 4
    }

    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;          // Recipient

        [Required]
        public string ActorUserId { get; set; } = string.Empty;     // Who triggered it

        public NotificationType Type { get; set; }

        public int? PostId { get; set; }
        public int? CommentId { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual User? User { get; set; }
        public virtual User? ActorUser { get; set; }
        public virtual Post? Post { get; set; }
        public virtual Comment? Comment { get; set; }
    }
}