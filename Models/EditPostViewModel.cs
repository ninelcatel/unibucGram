using System.ComponentModel.DataAnnotations;

namespace unibucGram.Models
{
    public class EditPostViewModel
    {
        public int PostId { get; set; }
        
        [Display(Name = "Content")]
        public string? Content { get; set; }
        
        [Display(Name = "Image URL")]
        public string? ImageURL { get; set; }
        
        public string AuthorUsername { get; set; } = string.Empty;
    }
}