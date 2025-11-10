using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace unibucGram.Models
{
    public class Follow
    {
        [Required]
        public string FollowerId { get; set; } = null!; // foreign key catre user (cine urmareste)
        public virtual User Follower { get; set; } = null!; // navigation catre user care urmareste

        [Required]
        public string FolloweeId { get; set; } = null!; // foreign key catre user (cine este urmarit)
        public virtual User Followee { get; set; } = null!; // navigation catre user care este urmarit

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // cand a fost creat follow-ul

        //aceeasi situatie ca la like,cheia primara compusa trebuie definita in dbContext
        //practic asa ne asiguram ca un user nu poate urmari de doua ori acelasi user
        //si previne auto-follow (FollowerId != FolloweeId)
    }
}