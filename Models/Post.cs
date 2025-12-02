using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;


namespace unibucGram.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }
        [StringLength(2000)]
        public string? Content { get; set; } = string.Empty; // e optionala descrierea

        public string? ImageURL { get; set; } = string.Empty; // url catre poza 
        public string? VideoURL { get; set; } = string.Empty; // url catre video
        public string? ThumbnailURL { get; set; } = string.Empty; // thumbnail pentru video
        public string? MediaType { get; set; } = string.Empty; // "image" sau "video"

        /*
        un post are poate sa nu aibe descriere sau poza/video, 
        dar trebuie sa aibe cel putin una din ele, 
        
        IDEE: poate se va verifica la nivel de formular?  
        */
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string UserId { get; set; } = null!;     // foreign key catre user (cine a postat) , adaugat null! si user ? pt warnings din IDE

        public virtual User? User { get; set; } // navigation catre user

        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>(); // navigation catre comments
        public virtual ICollection<Like> Likes { get; set; } = new List<Like>(); // navigation catre likes


    }
}