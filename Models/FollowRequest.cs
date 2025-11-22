using System;
using System.ComponentModel.DataAnnotations;

namespace unibucGram.Models
{
    public class FollowRequest
    {
        public int Id { get; set; }

        [Required]
        public string RequesterId { get; set; } // Who is sending the request

        [Required]
        public string RequesteeId { get; set; } // Who is receiving the request

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // ADD THESE NAVIGATION PROPERTIES
        public virtual User Requester { get; set; }
        public virtual User Requestee { get; set; }
    }
}