using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace unibucGram.Models
{
    public class Like
    {
        [Required]
        public string UserId { get; set; } = null!; // foreign key catre user (cine a dat like)
        public virtual User User { get; set; } = null!; // navigation catre user

        [Required]
        public int PostId { get; set; } // foreign key catre post
        public virtual Post Post { get; set; } = null!; // navigation catre post

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // cand a fost dat like-ul

        // agentu zice ca cica nu merge sa definesc aici cheia primara compusa,si trebuie definita in dbContext
    }
}