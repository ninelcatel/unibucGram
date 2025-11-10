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

        /*
        un post are poate sa nu aibe descriere sau poza, 
        dar trebuie sa aibe cel putin una din ele, 
        
        IDEE: poate se va verifica la nivel de formular?  
        */
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string UserId { get; set; } // foreign key catre user
        
        public virtual User User { get; set; } // navigation catre user

        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>(); // navigation catre comments
        public virtual ICollection<Like> Likes { get; set; } = new List<Like>(); // navigation catre likes


    }
}