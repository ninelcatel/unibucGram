using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace unibucGram.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Comment content is required")]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string UserId { get; set; } = null!; // foreign key catre user (cine a comentat)
        public virtual User User { get; set; } = null!; // navigation catre user

        [Required]
        public int PostId { get; set; } // foreign key catre post
        public virtual Post Post { get; set; } = null!; // navigation catre post
    }
}