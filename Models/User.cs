using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace unibucGram.Models
{
    public class User : IdentityUser
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        public string LastName { get; set; } 

        [Required(ErrorMessage = "Date of birth is required")]
        public DateTime DateOfBirth { get; set; }
        public bool isPrivate { get; set; } = false; // default public profile
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(200)]
        public string? Bio { get; set; } = string.Empty; // e optionala bio

        public string? PfpURL { get; set; } = string.Empty; // putem adauga un url pt basic pfp dar inca nu stiu cum ,vom vedea

        // lazy loading navigations
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<Follow> Followers { get; set; } = new List<Follow>();
        public virtual ICollection<Follow> Following { get; set; } = new List<Follow>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
        public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }  
}